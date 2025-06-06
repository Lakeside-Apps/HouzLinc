/* Copyright 2022 Christian Fortini

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System.Diagnostics;
using Common;
using Insteon.Model;
using ViewModel.Base;
using ViewModel.Settings;

namespace ViewModel.Devices;

// TODO: Create a list orderable on various fields ot DeviceViewModel (Id, Name, Location, etc.)
// Also ensure constant time lookup by id
public sealed class DeviceListViewModel : ItemListViewModel<DeviceViewModel>, IDevicesObserver, IRoomsObserver
{
    // A public default constructor is necessary to make the generated binding code compile
    // but it should never be called as we should always instantiate it with a list of devices.
    public DeviceListViewModel()
    {
        Debug.Assert(false, "DeviceListViewModel should always be created with a list of devices.");
        this.devices = null!;
    }

    private DeviceListViewModel(Insteon.Model.Devices devices, bool includeHub = false)
    {
        this.devices = devices;
        hasNoDevice = devices.Count == 0;
        RebuildList(includeHub);
    }

    /// <summary>
    /// Factory method
    /// </summary>
    /// <returns></returns>
    public static DeviceListViewModel Create(Insteon.Model.Devices devices, bool includeHub = false)
    {
        return new DeviceListViewModel(devices, includeHub);
    }

    /// <summary>
    /// Apply filter and sort order from the Settings store.
    /// </summary>
    public DeviceListViewModel ApplyFilterAndSortOrderFromSettings()
    {
        // Retrieve from the Settings store and apply last used sort
        SortOrder = SettingsStore.ReadLastUsedValueAsString("DevicesSortOrder") ?? string.Empty;

        // Retrieve last used room filter
        var roomFilter = SettingsStore.ReadLastUsedValueAsString("DevicesRoomFilter") ?? string.Empty;
        var rooms = Rooms;
        if (!rooms.Contains(roomFilter))
        {
            roomFilter = rooms.Count > 0 ? rooms[0] : string.Empty;
        }
        RoomFilter = roomFilter;
        return this;
    }

    // To idenfity items in the SettingsStore
    protected override string ItemTypeName => "Device";
    
    /// <summary>
    /// One-way bindable property 
    /// Whether the underlying device collection is empty
    /// </summary>
    public bool HasNoDevice
    {
        get => hasNoDevice;
        set
        {
            if (value != hasNoDevice)
            {
                hasNoDevice = value;
                OnPropertyChanged();
            }
        }
    }
    private bool hasNoDevice;

    /// <summary>
    /// The page using this has loaded
    /// </summary>
    public override void ViewLoaded()
    {
        devices.AddObserver(this);
        devices.House.Rooms.AddObserver(this);
    }

    /// <summary>
    /// The page using this has unloaded
    /// </summary>
    public override void ViewUnloaded()
    {
        devices.RemoveObserver(this);
        devices.House.Rooms.RemoveObserver(this);
    }

    /// <summary>
    /// Builds this list of DeviceViewModels from the model
    /// </summary>
    /// <param name="includeHub"></param>
    public void RebuildList(bool includeHub = false)
    {
        Items.Clear();

        // Build the list of DeviceViewModels 
        foreach (Device d in devices)
        {
            // The hub does not appear in the list
            if (!d.IsHub || includeHub)
            {
                Items.Add(DeviceViewModel.GetOrCreate(d));
            }
        }
    }


    /// <summary>
    /// Show new device dialog, using a callback into the UI layer
    /// </summary>
    public static event Func<Task<InsteonID?>> ShowDeviceIdDialogHandler = null!;
    internal static async Task<InsteonID?> ShowDeviceIdDialog()
    {
        if (ShowDeviceIdDialogHandler != null)
        {
            return await ShowDeviceIdDialogHandler.Invoke();
        }
        return null;
    }

    /// <summary>
    /// Bindable - Returns the list of sort orders
    /// Bindable to the UI
    /// </summary>
    public List<string> SortOrders
    {
        get 
        {
            if (sortOrders == null)
            {
                sortOrders = new List<string>();
                sortOrders.Add("");
                sortOrders.Add("Id");
                sortOrders.Add("Name");
                sortOrders.Add("Room");
            }   
            return sortOrders;
        }
    }
    private List<string>? sortOrders;

    /// <summary>
    /// Bindable - Device list sort order
    /// </summary>
    public string SortOrder
    {
        get => sortOrder;
        set
        {
            if (value != sortOrder)
            {
                sortOrder = value;
                OnPropertyChanged();
                ApplySortOrder();
                SettingsStore.WriteLastUsedValue("DevicesSortOrder", sortOrder);
            }
        }
    }
    private string sortOrder = string.Empty;

    // Apply current sort order to this view model
    private void ApplySortOrder()
    {
        // Remember the key of the selected item, if any
        var selectedItemKey = SelectedItem?.Id.ToString();

        switch (sortOrder)
        {
            case "Id":
                SortById(SortDirection.Ascending);
                break;
            case "Name":
                SortByName(SortDirection.Ascending);
                break;
            case "Room":
                SortByRoom(SortDirection.Ascending);
                break;
            default:
                // Go back to original order and repeat the room filter
                ApplyRoomFilter();
                break;
        }

        // Attempt to reselect the same item
        if (selectedItemKey != null)
        {
            TrySelectItemByKey(selectedItemKey);
        }
    }

    /// <summary>
    /// Bindable - List of rooms devices are in
    /// </summary>
    /// <returns></returns>
    public List<string> Rooms
    {
        get
        {
            if (rooms == null)
            {
                rooms = new List<string>(devices.House.Rooms);
                // TODO: localize
                rooms.Insert(0, "Any");
                rooms.Insert(1, "None");
            }
            return rooms;
        }
    }
    private List<string>? rooms;

    /// <summary>
    /// Bindable - Current Value filter
    /// </summary>
    public string RoomFilter
    {
        get => roomFilter;
        set
        {
            if (value != roomFilter)
            {
                roomFilter = value;
                OnPropertyChanged();
                ApplyRoomFilter();
                ApplySortOrder();
                SettingsStore.WriteLastUsedValue("DevicesRoomFilter", roomFilter);
            }
        }
    }
    private string roomFilter = string.Empty;

    // Apply the current room filter to the list
    // If argument is null, work off this view model
    private void ApplyRoomFilter()
    {
        // Remember the key of the selected item, if any
        var selectedItemKey = SelectedItem?.Id.ToString();

        // Reset and reapply filter
        RebuildList();
        FilterByRoom(RoomFilter);

        // If the selection was set before, attempt to reselect the same item,
        // or if it is not in the list anymore, default to the first item
        if (Items.Count > 0 && selectedItemKey != null && !TrySelectItemByKey(selectedItemKey))
            SelectedItem = Items.First();
    }

    /// <summary>
    /// Sort the list by different keys
    /// </summary>
    /// <param name="direction"></param>
    public void SortById(SortDirection direction)
    {
        Items.Sort<InsteonID>(x => x.Id, direction, new InsteonIDComparer());
    }

    public void SortByName(SortDirection direction)
    {
        Items.Sort<string>(x => x.DisplayName, direction);
    }

    public void SortByRoom(SortDirection direction)
    {
        Items.Sort<string>(x => x.Room, direction);
    }

    public void FilterByRoom(string room)
    {
        if (room == string.Empty || room == "Any")
        {
            return;
        }

        if (room == "None")
        {
            Items.Filter(x => x.Room == "" || x.Room == null || x.Room == "None");
            return;
        }

        Items.Filter(x => x.Room == room);
    }

    // Implementation of  IDevicesObserver
    // Update the ViewModel on change notifications from the model,
    // which will update the UI via data binding.

    void IDevicesObserver.DeviceAdded(Device device)
    {
        DeviceViewModel? deviceViewModel = DeviceViewModel.GetById(device.Id);
        if (deviceViewModel == null)
        {
            deviceViewModel = DeviceViewModel.GetOrCreateById(devices.House, device.Id);
            if (deviceViewModel != null)
                Items.Add(deviceViewModel);
        }

        if (deviceViewModel != null)
        {
            RoomsChanged();
            deviceViewModel.ActiveStateChanged();
        }
    }

    void IDevicesObserver.DeviceInserted(int seq, Device device)
    {
        // At the moment this only works for inserting the hub at the begining of the list
        // which has no impact on the view.
        if (seq != 0 || !device.IsHub)
            throw new NotImplementedException("Devices should be added at end of device list, inserting is not supported");
    }

    void IDevicesObserver.DeviceRemoved(Device device)
    {
        var index = GetItemIndexByKey(device.Id.ToString());
        if (index != -1)
        {
            Items.RemoveAt(index);
            RoomsChanged();
        }
    }

    void IDevicesObserver.DeviceTypeChanged(Device device)
    {
        // The device type may have changed and require a new type of view model.
        // (e.g., we preloaded a generic device view model for percieved responsiveness,
        // and it's the wrong type now)
        var deviceViewModel = DeviceViewModel.GetOrCreateById(device.House, device.Id);
        if (deviceViewModel != null)
        {
            var index = GetItemIndexByKey(deviceViewModel.ItemKey);
            if (index != -1)
            {
                if (!ReferenceEquals(Items[index], deviceViewModel))
                {
                    System.Diagnostics.Debug.Assert(Items[index].ItemKey == deviceViewModel.ItemKey);
                    Items[index] = deviceViewModel;
                }
            }
        }
    }

    /// <summary>
    /// Notification handler that the rooms list may have changed
    /// </summary>
    public void RoomsChanged()
    {
        rooms = null;
        OnPropertyChanged(nameof(Rooms));
    }

    // List of devices in the model
    private Insteon.Model.Devices devices;
}
