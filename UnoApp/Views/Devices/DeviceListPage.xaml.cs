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
    private async Task<InsteonID?> ShowNewDeviceDialog(InsteonID? showDeviceId, bool showPriorError = false)
    {
        if (XamlRoot == null)
        {
            throw new InvalidOperationException("XamlRoot is null");
        }

        NewDeviceDialog dialog = new NewDeviceDialog(XamlRoot, deviceListViewModel, showDeviceId, showPriorError);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            return dialog.DeviceId;
        }
        return null;
    }

    private async Task<InsteonID?> ShowNewDeviceDialog()
    {
        return await ShowNewDeviceDialog(showDeviceId: null);
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
        DeviceListViewModel.ShowNewDeviceDialogHandler += ShowNewDeviceDialog;
        DeviceListViewModel.ShowDeviceIdDialogHandler += ShowDeviceIdDialog;
    }

    protected override void OnPageUnloaded()
    {
        base.OnPageUnloaded();

        DeviceListViewModel.ShowNewDeviceDialogHandler -= ShowNewDeviceDialog;
        DeviceListViewModel.ShowDeviceIdDialogHandler -= ShowDeviceIdDialog;
    }

    private async void AddDeviceBtnClick(object sender, RoutedEventArgs e)
    {
        bool showPriorError = false;
        InsteonID? deviceId = null;
        while (true)
        {
            // Present the dialog to the user to enter/discover the device Id.
            // If successfull, this will add the new device to the model and
            // the corresponding new view model to this list if not already in.
            deviceId = await ShowNewDeviceDialog(deviceId, showPriorError);
            if (deviceId == null || deviceId.IsNull)
                break;

            var deviceViewModel = DeviceViewModel.GetOrCreateById(Holder.House, deviceId);
            if (deviceViewModel != null)
            {
                // If we had preloaded a vanilla device view model for percieved responsiveness,
                // remove it now
                if (!deviceListViewModel.Items.Empty() && 
                    deviceListViewModel.Items.Last().Id == deviceId)
                {
                    deviceListViewModel.Items.RemoveAt(deviceListViewModel.Items.Count - 1);
                }

                // Add the new device view model to the list showing on this page
                deviceListViewModel.Items.Add(deviceViewModel);

                // Select the new device
                SelectedItem = deviceViewModel;
                break;
            }

            showPriorError = true;
        }
    }

    private async void RemoveDeviceBtnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedItem != null)
        {
            var confirmDialog = new ConfirmDialog(XamlRoot)
            {
                // TODO: localize
                Title = $"About to Remove Device {SelectedItem.DisplayName}",
                Content = "Are you sure you want to remove this device?"
            };

            if (await confirmDialog.ShowAsync())
            {
                // Remove this device and unselect it
                deviceListViewModel.ScheduleRemoveDevice(SelectedItem.Id, success =>
                {
                    if (success)
                    {
                        SelectedItem = null;
                    }
                });
                
            }
        }
    }

    private void NavigateToHubSettings(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        (App.MainWindow.Content as AppShell)?.Navigate(typeof(HubSettingsPage), null, new DrillInNavigationTransitionInfo());
    }
}
