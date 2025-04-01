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

using ViewModel.Settings;
using ViewModel.Tools;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnoApp.Dialogs;
using ViewModel.Base;
using UnoApp.Views.Base;

namespace UnoApp.Views.Tools;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ToolsPage : PageWithViewModels
{
    // TODO: this should be made a string resource and fetch from the code and XAML as needed
    public string PageHeaderProperty => PageHeader;
    public const string PageHeader = "Tools";

    public ToolsPage()
    {
        this.InitializeComponent();
        AddViewModel(ToolsViewModel);
        AddViewModel(SettingsViewModel);
        AddViewModel(StatusBarViewModel);
    }

    // Main view model
    private ToolsViewModel ToolsViewModel => ToolsViewModel.Instance;

    // View model for the hub info
    private SettingsViewModel SettingsViewModel => SettingsViewModel.Instance;

    // View model for the status bar
    private StatusBarViewModel StatusBarViewModel => StatusBarViewModel.Instance;


    /// <summary>
    /// Handler for "Remove all references to an old device" link button
    /// ALlows the user to enter the device to remove
    /// </summary>
    private async void PromptAndRemoveDevice(object sender, RoutedEventArgs e)
    {
        var dialog = new ManualDeviceIdDialog(XamlRoot);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ToolsViewModel.ScheduleRemoveDevice(dialog.DeviceId!);
        }
    }
}
