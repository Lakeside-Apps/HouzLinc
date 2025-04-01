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
using ViewModel.Devices;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UnoApp.Dialogs;

public sealed partial class NewDeviceDialog : ContentDialog, INotifyPropertyChanged
{
    public NewDeviceDialog(XamlRoot xamlRoot, DeviceListViewModel deviceListViewModel, 
        InsteonID? showDeviceId = null, bool showPriorError = false)
    {
        this.InitializeComponent();
        this.Closing += DeviceIdDialog_Closing;
        this.deviceListViewModel = deviceListViewModel;
        this.XamlRoot = xamlRoot;
        this.canClose = true;
        this.DeviceId = showDeviceId;
        this.DeviceIdBox.Value = DeviceId;
        this.showPriorError = showPriorError;
    }

    // Data bdinding to the UI
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };
    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Add New Device button clicked
    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!wasAutoDiscovered)
        {
            // Create device and add it to the model, linking it to the Hub
            DeviceId = DeviceIdBox.Value;

            if (DeviceId == null)
            {
                canClose = true;
                return;
            }

            canClose = false;
            var job = deviceListViewModel.ScheduleAddOrConnectDevice(DeviceId,
                (d) =>
                {
                    if (d.deviceViewModel != null)
                    {
                        DeviceViewModel = d.deviceViewModel;
                        DeviceId = d.deviceViewModel.Id;
                        isNewDevice = d.isNew;
                    }

                    if (closingDeferral != null)
                    {
                        canClose = true;
                        closingDeferral.Complete();
                        closingDeferral = null;
                    }
                });

            if (job == null)
            {
                canClose = true;
            }
        }
        else
        {
            canClose = true;
        }
    }

    // Cancel button clicked
    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (isNewDevice && DeviceViewModel != null)
        {
            // We discovered a device, then user canceled, so remove it
            canClose = false;
            deviceListViewModel.ScheduleRemoveDevice(DeviceViewModel.Id,
                (success) =>
                {
                    if (success)
                    {
                        DeviceViewModel = null;
                    }

                    if (closingDeferral != null)
                    {
                        canClose = true;
                        closingDeferral.Complete();
                        closingDeferral = null;
                    }
                });
        }
        else if (autoDiscoveryJob != null)
        {
            // No point holding the dialog open since the user cancelled,
            // just fire and forget the cancellation
            deviceListViewModel.ScheduleCancelAddDevice(autoDiscoveryJob);
            canClose = true;
        }
        else 
        {
            canClose = true;
        }
    }

    void DeviceIdDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (!canClose && closingDeferral == null)
        {
            closingDeferral = args.GetDeferral();
        }
    }

    // Auto-discover Device button clicked
    // Start an IM linking operation 
    private void AutoDiscover_Click(object sender, RoutedEventArgs e)
    {
        autoDiscoveryJob = deviceListViewModel.ScheduleAddDeviceManually(
            (d) =>
            {
                if (d.deviceViewModel != null)
                {
                    DeviceViewModel = d.deviceViewModel;
                    DeviceId = d.deviceViewModel.Id;
                    DeviceIdBox.Value = d.deviceViewModel.Id;
                    wasAutoDiscovered = true;
                }
                autoDiscoveryJob = null;
            });
    }

    /// <summary>
    /// Device to add or auto-discovered and added, null if none
    /// </summary>
    public InsteonID? DeviceId
    {
        get => deviceId;
        private set
        {
            if (value != deviceId)
            {
                deviceId = value;
                OnPropertyChanged();
            }
        }
    }
    InsteonID? deviceId;

    // Whether we are in the process of auto-discovering the device,
    // i.e., the StartAllLinking command was called
    private bool isAutoDiscovering => autoDiscoveryJob != null;

    // Auto-discovery job (aka, Add device manually, StartAllLinking command)
    private object? autoDiscoveryJob
    {
        get => _autoDiscoveryJob;
        set
        {
            if (value != _autoDiscoveryJob)
            {
                _autoDiscoveryJob = value;
                if (value != null)
                    DeviceIdBox.Value = null;
                OnPropertyChanged();
                OnPropertyChanged(nameof(isAutoDiscovering));
            }
        }
    }
    private object? _autoDiscoveryJob;

    // DeviceViewModel for the device that is bing added
    private DeviceViewModel? DeviceViewModel;

    // List of deviceListViewModel to add new device to
    private readonly DeviceListViewModel deviceListViewModel;

    // Closing defferal to delay closing until the device is added
    private ContentDialogClosingDeferral? closingDeferral;

    // Whether the dialog can close
    private bool canClose;

    // Dialog should show an error indicating prior failure and asking user to try again
    private bool showPriorError;

    // whether device was auto-discovered
    private bool wasAutoDiscovered;

    // Discovered device is new 
    private bool isNewDevice;
}
