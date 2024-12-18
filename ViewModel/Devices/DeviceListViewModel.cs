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

using Common;
using Insteon.Model;
using ViewModel.Base;
using ViewModel.Settings;

namespace ViewModel.Devices;

// TODO: Create a list orderable on various fields ot DeviceViewModel (Id, Name, Location, etc.)
// Also ensure constant time lookup by id
public sealed class DeviceListViewModel : ItemListViewModel<DeviceViewModel>, IDevicesObserver, IRoomsObserver
{
    private DeviceListViewModel(Insteon.Model.Devices devices, bool includeHub = false)
    {
        this.devices = devices;
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

    /// <summary>
    /// Item collection
    /// </summary>
    public override SortableObservableCollection<DeviceViewModel> Items => items ??= new();
    private SortableObservableCollection<DeviceViewModel>? items;

    // To idenfity items in the SettingsStore
    protected override string ItemTypeName => "Device";

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
    /// Add a device to the network and to this collection by Id
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="completionCallback"></param>
    /// <param name="delay"></param>
    /// <returns>handle to scheduled job, null if the device was already in the network</returns>
    public object? ScheduleAddOrConnectDevice(InsteonID deviceId, 
        Scheduler.JobCompletionCallback<(DeviceViewModel? deviceViewModel, bool isNew)>? completionCallback = null, 
        TimeSpan delay = new TimeSpan())
    {
        // We create and add the device view model to this collection immediately,
        // to provide a  instant visual feedback to the user.
        DeviceViewModel? deviceViewModel = DeviceViewModel.GetById(deviceId);
        if (deviceViewModel == null)
        {
            deviceViewModel = DeviceViewModel.GetOrCreateById(devices.House, deviceId);
            if (deviceViewModel != null)
            {
                Items.Add(deviceViewModel);
            }
        }

        // If no device exists with this id or the device is not connected to the network, add and connect it now
        if (deviceViewModel == null || deviceViewModel.DeviceConnectionStatus != Device.ConnectionStatus.Connected)
        {
            return devices.ScheduleAddOrConnectDevice(deviceId, (d) =>
            {
                DeviceViewModel? deviceViewModel = null;
                if (d.device != null)
                {
                    deviceViewModel = DeviceViewModel.GetById(d.device.Id);
                }
                else if (deviceViewModel != null)
                {
                    // TODO: Should we remove the device view model from the collection on failure to create the device?
                    // Or is it better to leave it there marked disconnected?
                    Items.Remove(deviceViewModel);
                }
                completionCallback?.Invoke((deviceViewModel, d.isNew));
            },
            delay);
        }

        return null;
    }

    /// <summary>
    /// Schedule removing a device from the network, from the model and from this collection
    /// </summary>
    /// <param name="deviceId">id of device to remove</param>
    /// <param name="completionCallback"></param>
    /// <param name="delay"></param>
    /// <returns>handle to scheduled job</returns>
    public Object? ScheduleRemoveDevice(InsteonID deviceId, Scheduler.JobCompletionCallback<bool>? completionCallback = null, TimeSpan delay = new TimeSpan())
    {
        return devices.House.Devices.ScheduleRemoveDevice(deviceId, completionCallback, delay);
    }

    /// <summary>
    /// Schedule adding a new device manually to the network, i.e., the user has to press the SET button for 3 sec on the actual device
    /// If successful, update properties and links from the new device and links the hub
    /// </summary>
    /// <param name="completionCallback">called on completion if not null</param>
    /// <param name="delay">delay before running job</param>
    /// <returns>handle to scheduled job</returns>
    public object ScheduleAddDeviceManually(Scheduler.JobCompletionCallback<(DeviceViewModel? deviceViewModel, bool isNew)>? completionCallback = null, TimeSpan delay = new TimeSpan())
    {
        return devices.ScheduleAddDeviceManually((d) =>
            {
                DeviceViewModel? deviceViewModel = null;
                if (d.device != null)
                {
                    deviceViewModel = DeviceViewModel.GetById(d.device.Id);
                }
                completionCallback?.Invoke((deviceViewModel, d.isNew));
            },
            delay);
    }

    /// <summary>
    /// Show new device dialog, using a callback into the UI layer
    /// </summary>
    internal static async Task<InsteonID?> ShowNewDeviceDialog()
    {
        if (ShowNewDeviceDialogHandler != null)
        {
            return await ShowNewDeviceDialogHandler.Invoke();
        }
        return null;
    }

    /// <summary>
    /// Event to UI layer to show NewDeviceDialog
    /// </summary>
    public static event Func<Task<InsteonID?>> ShowNewDeviceDialogHandler = null!;

    /// <summary>
    /// Show new device dialog, using a callback into the UI layer
    /// </summary>
    internal static async Task<InsteonID?> ShowDeviceIdDialog()
    {
        if (ShowDeviceIdDialogHandler != null)
        {
            return await ShowDeviceIdDialogHandler.Invoke();
        }
        return null;
    }

    /// <summary>
    /// Event to UI layer to show DeviceIdDialog
    /// </summary>
    /// <returns></returns>
    public static event Func<Task<InsteonID?>> ShowDeviceIdDialogHandler = null!;

    /// <summary>
    /// Cancels adding a device manually
    /// </summary>
    /// <param name="job">Add device job to cancel</param>
    /// <returns>handle to scheduled job</returns>
    public object ScheduleCancelAddDevice(object job)
    {
        return devices.ScheduleCancelAddDevice(job);
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
        throw new NotImplementedException("Devices should be added at end of device list, inserting is not supported");
    }

    void IDevicesObserver.DeviceRemoved(Device device)
    {
        var index =- GetItemIndexByKey(device.Id.ToString());
        if (index != -1)
        {
            Items.RemoveAt(index);
            RoomsChanged();
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
