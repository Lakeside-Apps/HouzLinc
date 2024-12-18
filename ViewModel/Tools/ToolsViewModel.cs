/* Copyright 2022 ChristianGa Fortini
/* Copyright 2022 ChristianGa Fortini

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Insteon.Model;
using Common;
using ViewModel.Settings;
using ViewModel.Base;

namespace ViewModel.Tools;
public class ToolsViewModel : PageViewModel
{
    /// <summary>
    /// Must be called before using this class
    /// </summary>
    public ToolsViewModel()
    {
    }

    public static ToolsViewModel Instance => instance ??= new ToolsViewModel();
    private static ToolsViewModel? instance;

    // Initialize the bindable state when the page is loaded
    public override void ViewLoaded()
    {
        OnPropertyChanged(nameof(HasOldGatewayToRemove));
        OnPropertyChanged(nameof(NextOldGatewayToRemove));
    }

    public override void ViewUnloaded()
    {
    }

    /// <summary>
    /// Remove the last hub/gateway in the list of gateways, if the list has more than one gateway
    /// </summary>
    public void ScheduleRemoveOldGateway()
    {
        RemoveOldGatewayJob = Holder.House.ScheduleRemoveOldGateway((success) => {
            IsRemoveOldGatewayRunning = false;
            OnPropertyChanged(nameof(HasOldGatewayToRemove));
            OnPropertyChanged(nameof(NextOldGatewayToRemove));
        });
        IsRemoveOldGatewayRunning = true;
    }

    /// <summary>
    /// Cancel removing last gateway on the list of gateways
    /// </summary>
    public void CancelRemoveOldGateway()
    {
        House.CancelJob(RemoveOldGatewayJob);
    }
    private static object? RemoveOldGatewayJob;

    /// <summary>
    /// Used to show progress and cancel UX while "Connect all devices to hub" command is running
    /// </summary>
    public bool IsRemoveOldGatewayRunning
    {
        get => isRemoveOldHubRunning;
        private set
        {
            if (value != isRemoveOldHubRunning)
            {
                isRemoveOldHubRunning = value;
                OnPropertyChanged(nameof(IsRemoveOldGatewayRunning));
            }
        }
    }
    private bool isRemoveOldHubRunning;

    /// <summary>
    /// Are there any old gateway to remove?
    /// </summary>
    public bool HasOldGatewayToRemove => Holder.House?.Gateways.Count > 1;

    /// <summary>
    /// Insteon ID of the next gateway to remove
    /// </summary>
    public InsteonID NextOldGatewayToRemove => Holder.House?.Gateways.Count > 1 ? Holder.House.Gateways[Holder.House.Gateways.Count - 1].DeviceId : InsteonID.Null;


    /// <summary>
    /// Force sync all devices in the model with their physical couterparts on the network
    /// </summary>
    public void ScheduleSyncAllDevices()
    {
        Holder.House.Devices.SyncDevicesNow((success) =>
        {
            IsSyncAllDevicesRunning = false;
        },
        forceRead: true);
        IsSyncAllDevicesRunning = true;
    }

    /// <summary>
    /// Cancel force syncing properties and database of all devices on the network
    /// This will stop before the next device to read and will restart normal syncing of devices
    /// </summary>
    public void CancelSyncAllDevices()
    {
        Holder.House.Devices.CancelBackgroundDeviceSync();
    }

    public bool IsSyncAllDevicesRunning
    {
        get => isSyncAllDevicesNowRunning;
        set
        {
            if (value != isSyncAllDevicesNowRunning)
            {
                isSyncAllDevicesNowRunning = value;
                OnPropertyChanged(nameof(IsSyncAllDevicesRunning));
            }
        }
    }
    private bool isSyncAllDevicesNowRunning;


    /// <summary>
    /// Remove all references to an old device
    /// </summary>
    public void ScheduleRemoveDevice(InsteonID deviceId)
    {
        // Remove links referring to the device
        Holder.House.Devices.ScheduleRemoveDevice(deviceId, (success) =>
        {
            Holder.House.Devices.SyncDevicesNow((success) =>
            {
                IsRemoveDeviceRunning = false;
            },
            forceRead: true);
        });
        IsRemoveDeviceRunning = true;
    }

    public bool IsRemoveDeviceRunning
    { 
        get => isRemoveDeviceRunning;
        private set
        {
            if (value != isRemoveDeviceRunning)
            {
                isRemoveDeviceRunning = value;
                OnPropertyChanged(nameof(IsRemoveDeviceRunning));
            }
        }
    }
    private bool isRemoveDeviceRunning;


    /// <summary>
    /// Handler for the "Transfer Hub" menu
    /// Transfer the database of the previous hub to the new one 
    /// and fixes up all links to old hub to point to the new one
    /// </summary>
    public void ScheduleTransferHub()
    {
        // TODO: this method needs work
        // TODO: copy old hub database into new one
        var hub = Holder.House.Hub;

        // Write hub and devices database to the network
        hub?.ScheduleWriteAllLinkDatabase((success) =>
        {
            if (success)
            {
                // Write the Hub All-Link database to the new Hub
                hub.FixupLinksToThis(hub.Id);
            }
        });
    }


    /// <summary>
    /// Import all devices and their link and properties from the Insteon network
    /// </summary>
    public void ScheduleImportAllDevices()
    {
        Holder.House.Devices.ScheduleImportAllDevices((success) =>
        {
            IsImportAllDevicesRunning = false;
        });
        IsImportAllDevicesRunning = true;
    }

    /// <summary>
    /// Cancel reImport all devices to the gateway
    /// This will stop on the next device to Import, already Imported devices stay that way
    /// </summary>
    public void CancelImportAllDevices()
    {
        Holder.House.Devices.CancelImportAllDevices();
    }

    public bool IsImportAllDevicesRunning
    {
        get => isImportAllDevicesRunning;
        set
        {
            if (value != isImportAllDevicesRunning)
            {
                isImportAllDevicesRunning = value;
                OnPropertyChanged(nameof(IsImportAllDevicesRunning));
            }
        }
    }
    private bool isImportAllDevicesRunning;


    /// <summary>
    /// Reconnect all devices to the gateway
    /// </summary>
    public void ScheduleConnectAllDevices()
    {
        Holder.House.Devices.ScheduleConnectAllDevices((success) =>
        {
            IsConnectAllDevicesRunning = false;
        });
        IsConnectAllDevicesRunning = true;
    }

    /// <summary>
    /// Cancel reconnect all devices to the gateway
    /// This will stop on the next device to connect, already connected devices stay that way
    /// </summary>
    public void CancelConnectAllDevices()
    {
        Holder.House.Devices.CancelConnectAllDevices();
    }

    public bool IsConnectAllDevicesRunning
    {
        get => isConnectAllDevicesRunning;
        set
        {
            if (value != isConnectAllDevicesRunning)
            {
                isConnectAllDevicesRunning = value;
                OnPropertyChanged(nameof(IsConnectAllDevicesRunning));
            }
        }
    }
    private bool isConnectAllDevicesRunning;


    /// <summary>
    /// Ensure all device have a link to the hub in their first database entry
    /// This appears to be required to get consistent responses from devices and not get NAKs
    /// </summary>
    public void EnsureLinkToHubIsFirst()
    {
        Holder.House.Devices.EnsureLinkToHubIsFirst();
    }


    /// <summary>
    /// Purge wacky links in the hub/gateway 
    /// </summary>
    public void SchedulePurgeHubLinks()
    {
        Holder.House.Hub?.SchedulePurgeIMLinks(null);
    }
}
