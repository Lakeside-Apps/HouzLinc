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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using ViewModel.Settings;
using ViewModel.Base;
using HouzLinc.Views.Base;

namespace HouzLinc.Views.Settings;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : PageWithViewModels
{
    // TODO: this should be made a string resource and fetch from the code and XAML as needed
    public string PageHeaderProperty => PageHeader;
    public const string PageHeader = "Settings";

    public SettingsPage()
    {
        this.InitializeComponent();

        AddViewModel(SettingsViewModel);
        AddViewModel(StatusBarViewModel);
    }

    // View Models
    private SettingsViewModel SettingsViewModel => SettingsViewModel.Instance;
    private StatusBarViewModel StatusBarViewModel => StatusBarViewModel.Instance;
}
