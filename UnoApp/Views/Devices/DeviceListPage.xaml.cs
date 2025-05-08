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
using Common;
using ViewModel.Devices;
using ViewModel.Settings;
using ViewModel.Tools;
using Microsoft.UI.Xaml.Media.Animation;
using UnoApp.Views.Settings;

namespace UnoApp.Views.Devices;

/// <summary>
/// Base class for the link master detail page 
/// Used in the XAML as the main page control type
/// </summary>
public abstract partial class DeviceListPageBase : ItemListPage<DeviceViewModel, DeviceDetailsPage>
{
}

/// <summary>
/// Link master detail page
/// </summary>
public sealed partial class DeviceListPage : DeviceListPageBase
{
    // TODO: this should be made a string resource and fetch from the code and XAML as needed
    public string PageHeaderProperty => PageHeader;
    public const string PageHeader = "Devices";

    public DeviceListPage()
    {
        this.InitializeComponent();
    }

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

    private async Task<InsteonID?> ShowDeviceIdDialog()
    {
        if (XamlRoot == null)
        {
            throw new InvalidOperationException("XamlRoot is null");
        }

        DeviceIdDialog dialog = new DeviceIdDialog(XamlRoot);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            return dialog.DeviceId;
        }
        return null;
    }

    // Main view model
    protected override DeviceListViewModel ItemListViewModel => 
        itemsListViewModel ??= DeviceListViewModel.Create(Holder.House.Devices)
            .ApplyFilterAndSortOrderFromSettings();
    private DeviceListViewModel? itemsListViewModel;
    private DeviceListViewModel deviceListViewModel => (ItemListViewModel as DeviceListViewModel)!;

    // Additional view models referenced on this page
    private SettingsViewModel settingsViewModel => SettingsViewModel.Instance;
    private ToolsViewModel toolsViewModel => ToolsViewModel.Instance;

    // Control accessors for base page
    protected override ListView ItemListView => DeviceListView;
    protected override ContentControl ItemDetailsPresenter => DeviceDetailsPresenter;
    protected override VisualStateGroup PageSizeVisualStateGroup => PageSizeStatesGroup;
    protected override VisualStateGroup MasterDetailVisualStateGroup => MasterDetailsStatesGroup;

    protected override void OnPageLoaded()
    {
        base.OnPageLoaded();

        // Setup callback into the UI layer for ViewModels to show dialogs
        DeviceListViewModel.ShowDeviceIdDialogHandler += ShowDeviceIdDialog;
    }

    protected override void OnPageUnloaded()
    {
        base.OnPageUnloaded();

        DeviceListViewModel.ShowDeviceIdDialogHandler -= ShowDeviceIdDialog;
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

                SelectedItem = deviceViewModel;
                break;
            }

            // We could not add the device (e.g., device with the Id the user entered
            // did not exist on the network). Try again.
            showPriorError = true;
        }
    }

    // Handler for the "Remove Device" button
    private async void RemoveDevice_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem != null)
        {
            var confirmDialog = new ConfirmDialog(XamlRoot)
            {
                // TODO: localize
                Title = $"About to Remove Device {SelectedItem.DisplayNameAndId}",
                Content = "Are you sure you want to remove this device?"
            };

            if (await confirmDialog.ShowAsync())
            {
                // Remove this device and unselect it
                SelectedItem.ScheduleRemoveDevice(success =>
                {
                    if (success)
                    {
                        SelectedItem = null;
                    }
                });
                
            }
        }
    }

    // Handler for the "Replace Device" menu item
    private async void ReplaceDevice_Click(object sender, RoutedEventArgs e)
    {
        bool showPriorError = false;
        InsteonID? replacementDeviceId = null;
        while (true)
        {
            // Present the dialog to the user to enter/discover the id of the replacement device.
            // If successfull, this will add the new device to the model and
            // the corresponding new view model to this list if not already in.
            replacementDeviceId = await ShowNewDeviceDialog($"Replace {SelectedItem?.DisplayNameAndId} by Device", "Replace", replacementDeviceId, showPriorError);
            if (replacementDeviceId == null || replacementDeviceId.IsNull)
                break;

            // If we have a view model for the replacement device, proceed with the replacement
            var relacementDeviceViewModel = DeviceViewModel.GetOrCreateById(Holder.House, replacementDeviceId);
            if (relacementDeviceViewModel != null && SelectedItem != null)
            {
                SelectedItem.ReplaceDevice(replacementDeviceId);
                SelectedItem = relacementDeviceViewModel;
                break;
            }

            // We could not add the device (e.g., device with the Id the user entered
            // did not exist on the network). Try again.
            showPriorError = true;
        }
    }

    // Handler for the "Copy Device" menu item
    private async void CopyDevice_Click(object sender, RoutedEventArgs e)
    {
        bool showPriorError = false;
        InsteonID? copyDevice = null;
        while (true)
        {
            // See ReplaceDevice_Click for comments
            copyDevice = await ShowNewDeviceDialog($"Copy {SelectedItem?.DisplayNameAndId} to Device", "Copy", copyDevice, showPriorError);
            if (copyDevice == null || copyDevice.IsNull)
                break;

            var copyDeviceViewModel = DeviceViewModel.GetOrCreateById(Holder.House, copyDevice);
            if (copyDeviceViewModel != null && SelectedItem != null)
            {
                SelectedItem.CopyDevice(copyDevice);
                SelectedItem = copyDeviceViewModel;
                break;
            }

            showPriorError = true;
        }

    }

    private void NavigateToHubSettings(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        (App.MainWindow.Content as AppShell)?.Navigate(typeof(HubSettingsPage), null, new DrillInNavigationTransitionInfo());
    }
}
