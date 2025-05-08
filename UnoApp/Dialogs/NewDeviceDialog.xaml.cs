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
using Insteon.Model;

namespace UnoApp.Dialogs;

public sealed partial class NewDeviceDialog : ContentDialog, INotifyPropertyChanged
{
    public NewDeviceDialog(XamlRoot xamlRoot, House house, string title, string primaryButtonText,
        InsteonID? showDeviceId = null, bool showPriorError = false)
    {
        this.InitializeComponent();
        this.Closing += DeviceIdDialog_Closing;
        this.house = house;
        this.XamlRoot = xamlRoot;
        this.canClose = true;
        this.DeviceId = showDeviceId;
        this.DeviceIdBox.Value = DeviceId;
        this.showPriorError = showPriorError;
        this.Title = title;
        this.PrimaryButtonText = primaryButtonText;
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
            var job = DeviceViewModel.ScheduleAddOrConnectDevice(DeviceId, house,
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
            DeviceViewModel.ScheduleRemoveDevice(
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
            DeviceViewModel.ScheduleCancelAddDevice(autoDiscoveryJob, house);
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
        autoDiscoveryJob = DeviceViewModel.ScheduleAddDeviceManually(house,
            (d) =>
            {
                if (d.deviceViewModel != null)
                {
                    DeviceViewModel = d.deviceViewModel;
                    DeviceId = d.deviceViewModel.Id;
                    DeviceIdBox.Value = d.deviceViewModel.Id;
                    isNewDevice = d.isNew;
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
                OnPropertyChanged(nameof(isInputEnabledAndValueValid));
            }
        }
    }
    InsteonID? deviceId;

    /// <summary>
    /// Private property tracking DeviceIdBox.IsValueValid.
    /// so that we can show the Add button when the value is valid.
    /// </summary>
    private bool isValueValid
    {
        get => _isValueValid;
        set
        {
            if (_isValueValid != value)
            {
                _isValueValid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(isInputEnabledAndValueValid));
            }
        }
    }
    private bool _isValueValid;
    
    // Whether input is enabled in the dialog.
    // It is not when autodiscovering or waiting to close.
    private bool isInputEnabled => autoDiscoveryJob == null && canClose;
    private bool isInputEnabledAndValueValid => isInputEnabled && isValueValid;

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
                OnPropertyChanged(nameof(isInputEnabled));
                OnPropertyChanged(nameof(isInputEnabledAndValueValid));
            }
        }
    }
    private object? _autoDiscoveryJob;

    // DeviceViewModel for the device that is bing added
    private DeviceViewModel? DeviceViewModel;

    // Model to which the device is to be added
    private readonly House house;

    // Closing defferal to delay closing until the device is added
    private ContentDialogClosingDeferral? closingDeferral;

    // Whether the dialog can close
    private bool canClose
    {
        get => _canClose;
        set
        {
            if (value != _canClose)
            {
                _canClose = value;
                OnPropertyChanged(nameof(isInputEnabled));
                OnPropertyChanged(nameof(isInputEnabledAndValueValid));
            }
        }
    }
    private bool _canClose;

    // Dialog should show an error indicating prior failure and asking user to try again
    private bool showPriorError;

    // whether device was auto-discovered
    private bool wasAutoDiscovered;

    // Discovered device is new 
    private bool isNewDevice;
}
