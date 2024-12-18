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

using HouzLinc.Views.Base;
using HouzLinc.Dialogs;
using ViewModel.Devices;
using ViewModel.Settings;

namespace HouzLinc.Views.Devices;

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
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async void AddDeviceBtnClick(object sender, RoutedEventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        // Current not implemented - Detail page does not have Add button
        throw new NotImplementedException();
    }

    private async void RemoveDeviceBtnClick(object sender, RoutedEventArgs e)
    {
        if (ItemViewModel != null)
        {
            var confirmDialog = new ConfirmDialog(XamlRoot)
            {
                // TODO: localize
                Title = $"About to Remove Device {ItemViewModel.DisplayName}",
                Content = "Are you sure you want to remove this device?"
            };

            if (await confirmDialog.ShowAsync())
            {
                // Remove device from the model and navigate away from its view
                ItemViewModel.RemoveDevice(success =>
                {
                    if (success)
                    {
                        (App.MainWindow.Content as AppShell)?.GoBackNoContext();
                    }
                });
            }
        }
    }
}
