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

using UnoApp.Views.Base;
using UnoApp.Dialogs;
using ViewModel.Devices;
using ViewModel.Settings;
using Common;
using Microsoft.UI.Xaml.Media.Animation;

namespace UnoApp.Views.Devices;

/// <summary>
/// Base class for the link details page 
/// Used in the XAML as the main page control type
/// </summary>
public abstract partial class DeviceDetailsPageBase : ItemDetailsPage<DeviceViewModel, DeviceListPage>
{
}


/// <summary>
/// Link details page
/// </summary>
public sealed partial class DeviceDetailsPage : DeviceDetailsPageBase
{
    public DeviceDetailsPage()
    {
        this.InitializeComponent();
    }

    protected override DeviceViewModel? GetOrCreateItemByKey(string itemKey)
    {
        return DeviceViewModel.GetOrCreateItemByKey(Holder.House, itemKey);
    }

    protected override ContentControl ItemDetailsPresenter => DeviceDetailsPresenter;

    // TODO: this should be made a string resource and fetch from the code and XAML as needed
    public string PageHeaderProperty => PageHeader;
    public const string PageHeader = "Device";

    // TODO: Consider moving these to a more global place such as the MainWindow for example.
    // showDeviceId prepopulates the dialog with the given device ID, null to not prepopulate.
    // showPriorError shows an error message if the dialog was shown before and the device ID could not be resolved.
    private async Task<InsteonID?> ShowNewDeviceDialog(string title, string primaryButtonText, InsteonID ? showDeviceId, bool showPriorError = false)
    {
        if (XamlRoot == null)
        {
            throw new InvalidOperationException("XamlRoot is null");
        }

        NewDeviceDialog dialog = new NewDeviceDialog(XamlRoot, Holder.House, title, primaryButtonText, showDeviceId, showPriorError);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            return dialog.DeviceId;
        }
        return null;
    }

    // Handler for the "Add device" button
    private async void AddDevice_Click(object sender, RoutedEventArgs e)
    {
        bool showPriorError = false;
        InsteonID? deviceId = null;
        while (true)
        {
            // Present the dialog to the user to enter/discover the device Id.
            // If successfull, this will add the new device to the model and
            // add the corresponding new view model to this list if not already in.
            deviceId = await ShowNewDeviceDialog("Add New Device", "Add", deviceId, showPriorError);
            if (deviceId == null || deviceId.IsNull)
            {
                // User cancelled out of the dialog
                break;
            }

            // If we have a new device view model, select it and return
            var deviceViewModel = DeviceViewModel.GetOrCreateById(Holder.House, deviceId);
            if (deviceViewModel != null)
            {
                // Navigate to the detail page for the copied device
                Frame.Navigate(typeof(DeviceDetailsPage), deviceViewModel.ItemKey, new DrillInNavigationTransitionInfo());
                break;
            }

            // We could not add the device (e.g., device with the Id the user entered
            // did not exist on the network). Try again.
            showPriorError = true;
        }
    }

    private async void RemoveDevice_Click(object sender, RoutedEventArgs e)
    {
        if (ItemViewModel != null)
        {
            var confirmDialog = new ConfirmDialog(XamlRoot)
            {
                // TODO: localize
                Title = $"About to Remove Device {ItemViewModel.DisplayNameAndId}",
                Content = "Are you sure you want to remove this device?"
            };

            if (await confirmDialog.ShowAsync())
            {
                // Remove device from the model and navigate away from its view
                ItemViewModel.ScheduleRemoveDevice(success =>
                {
                    if (success)
                    {
                        (App.MainWindow.Content as AppShell)?.GoBackNoContext();
                    }
                });
            }
        }
    }

    // Handler for the "Replace Device" menu
    public async void ReplaceDevice_Click(object sender, RoutedEventArgs e)
    {
        bool showPriorError = false;
        InsteonID? replacementDeviceId = null;
        while (true)
        {
            // Present the dialog to the user to enter/discover the id of the replacement device.
            // If successfull, this will add the new device to the model and
            // the corresponding new view model to this list if not already in.
            replacementDeviceId = await ShowNewDeviceDialog($"Replace {ItemViewModel?.DisplayNameAndId} by Device", "Replace", replacementDeviceId, showPriorError);
            if (replacementDeviceId == null || replacementDeviceId.IsNull)
                break;

            // If we have a view model for the replacement device, proceed with the replacement
            var replacementDeviceViewModel = DeviceViewModel.GetOrCreateById(Holder.House, replacementDeviceId);
            if (replacementDeviceViewModel != null && ItemViewModel != null)
            {
                ItemViewModel.ReplaceDevice(replacementDeviceId);
                // Navigate to the detail page for the replaced device
                Frame.Navigate(typeof(DeviceDetailsPage), replacementDeviceViewModel.ItemKey, new DrillInNavigationTransitionInfo());
                break;
            }

            // We could not add the device (e.g., device with the Id the user entered
            // did not exist on the network). Try again.
            showPriorError = true;
        }
    }

    // Handler for the "Copy Device" menu
    public async void CopyDevice_Click(object sender, RoutedEventArgs e)
    {
        bool showPriorError = false;
        InsteonID? copyDeviceId = null;
        while (true)
        {
            // See ReplaceDevice_Click for comments
            copyDeviceId = await ShowNewDeviceDialog($"Copy {ItemViewModel?.DisplayNameAndId} to Device", "Copy", copyDeviceId, showPriorError);
            if (copyDeviceId == null || copyDeviceId.IsNull)
                break;

            var copyDeviceViewModel = DeviceViewModel.GetOrCreateById(Holder.House, copyDeviceId);
            if (copyDeviceViewModel != null && ItemViewModel != null)
            {
                ItemViewModel.CopyDevice(copyDeviceId);
                // Navigate to the detail page for the copy
                Frame.Navigate(typeof(DeviceDetailsPage), copyDeviceViewModel.ItemKey, new DrillInNavigationTransitionInfo());
                break;
            }

            showPriorError = true;
        }
    }

}
