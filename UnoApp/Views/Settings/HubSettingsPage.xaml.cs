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
using ViewModel.Settings;
using ViewModel.Base;
using Microsoft.UI.Xaml.Media.Animation;
using UnoApp.Views.Devices;

namespace UnoApp.Views.Settings;

public sealed partial class HubSettingsPage : PageWithViewModels
{
    // TODO: this should be made a string resource and fetch from the code and XAML as needed
    public string PageHeaderProperty => PageHeader;
    public const string PageHeader = "Hub Settings";

    public HubSettingsPage()
    {
        this.InitializeComponent();

        AddViewModel(SettingsViewModel);
        AddViewModel(StatusBarViewModel);
    }

    // View Models
    private SettingsViewModel SettingsViewModel => SettingsViewModel.Instance;
    private StatusBarViewModel StatusBarViewModel => StatusBarViewModel.Instance;

    private void NavigateToDevices(object sender, RoutedEventArgs e)
    {
        (App.MainWindow.Content as AppShell)?.Navigate(typeof(DeviceListPage), null, new DrillInNavigationTransitionInfo());
    }
}
