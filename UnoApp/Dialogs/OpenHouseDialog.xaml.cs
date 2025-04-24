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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UnoApp.Dialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class OpenHouseDialog : ContentDialog
{
    public OpenHouseDialog(XamlRoot? xamlRoot, bool showPreviousError = false)
    {
        this.InitializeComponent();
        this.XamlRoot = xamlRoot;
        IsPrimaryButtonEnabled = false; 
        ShowPreviousError = showPreviousError;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Action = SettingsViewModel.OpenHouseDialogActions.Exit;
    }

    public SettingsViewModel.OpenHouseDialogActions Action;

    public bool ShowPreviousError { get; set; } = false;

    private void Option_Checked(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.Assert(sender is RadioButton);
        if ((sender is RadioButton rb) && rb.IsChecked == true)
        {
            switch (rb.Name)
            {
                case "File":
                    Action = SettingsViewModel.OpenHouseDialogActions.File;
                    IsPrimaryButtonEnabled = true;
                    break;
                case "New":
                    Action = SettingsViewModel.OpenHouseDialogActions.New;
                    IsPrimaryButtonEnabled = true;
                    break;
                case "OneDrive":
                    Action = SettingsViewModel.OpenHouseDialogActions.OneDrive;
                    IsPrimaryButtonEnabled = true;
                    break;
            }
        }
    }
}
