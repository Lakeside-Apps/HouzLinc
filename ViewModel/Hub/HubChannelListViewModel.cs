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
using ViewModel.Base;
using ViewModel.Settings;
using Insteon.Model;
using Common;

namespace ViewModel.Hub;

public sealed class HubChannelListViewModel : ItemListViewModel<HubChannelViewModel>
{
    private HubChannelListViewModel(Device? hub)
    {
        this.hub = hub;
        RebuildList();

        // Retrieve from the Settings store and apply last used sort order and room filter
        SortOrder = SettingsStore.ReadLastUsedValueAsString("HubChannelsSortOrder") ?? string.Empty;
    }

    public static HubChannelListViewModel Create()
    {
        Device? hub = null;

        if (Holder.House.Devices.Count > 0)
        {
            hub = Holder.House.Devices[0];
            Debug.Assert(hub == null || hub.IsHub);
        }

        return new HubChannelListViewModel(hub);
    }

    // To identify items in the SettingsStore
    protected override string ItemTypeName => "Scene";

    public override void ViewLoaded()
    {
        if (hub != null)
        {
            deviceReadJob = House.ScheduleGroupJob("Reading Hub " + hub.DisplayNameAndId,
                (success) => { deviceReadJob = null; });

            // If we have not already, schedule a read of the physcial device properties
            hub.ScheduleReadDeviceProperties(
                completionCallback: null,
                group: deviceReadJob,
                delay: TimeSpan.FromSeconds(10),
                priority: Scheduler.Priority.Low,
                forceSync: false);

            // Schedule a read of the link database if we think we may not have the latest
            hub.ScheduleReadAllLinkDatabase(
                completionCallback: null,
                group: deviceReadJob,
                delay: TimeSpan.FromSeconds(10),
                priority: Scheduler.Priority.Low);
        }
    }

    public override void ViewUnloaded()
    {
        // Cancel any pending network read jobs for this device since we are moving away from it
        Device.CancelScheduledJob(deviceReadJob);
    }

    private object? deviceReadJob;

    /// <summary>
    /// Builds this list of DeviceViewModels from the model
    /// </summary>
    public void RebuildList()
    {
        Items.Clear();

        if (hub != null)
        {
            foreach (Channel hubChannel in hub.Channels)
            {
                Items.Add(HubChannelViewModel.GetOrCreate(hub, hubChannel));
            }
        }
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
            }
            return sortOrders;
        }
    }
    private List<string>? sortOrders;

    // On way bindable to UI
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
                SettingsStore.WriteLastUsedValue("HubChannelsSortOrder", sortOrder);
            }
        }
    }
    private string sortOrder = string.Empty;

    // Apply current sort to the list
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
            default:
                // go back to original order
                RebuildList();
                break;
        }

        // Attempt to reselect the same item
        if (selectedItemKey != null)
            TrySelectItemByKey(selectedItemKey);
    }

    /// <summary>
    /// Sort the list by different keys
    /// </summary>
    /// <param name="direction"></param>
    public void SortById(SortDirection direction)
    {
        Items.Sort<int>(x => x.Id, direction);
    }

    public void SortByName(SortDirection direction)
    {
        Items.Sort<string>(x => x.Name ?? string.Empty, direction);
    }

    private Device? hub;
}
