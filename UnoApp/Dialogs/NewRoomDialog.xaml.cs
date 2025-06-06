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

namespace UnoApp.Dialogs;

public sealed partial class NewRoomDialog : ContentDialog
{
    public NewRoomDialog(XamlRoot? xamlRoot)
    {
        this.InitializeComponent();
        XamlRoot = xamlRoot;
        IsPrimaryButtonEnabled = false;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Room = EditBox.Text;
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView lv && lv.SelectedItem is TextBlock tb)
        {
            EditBox.Text = tb.Text;
            IsPrimaryButtonEnabled = tb.Text != string.Empty;
        }
    }

    private void TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            IsPrimaryButtonEnabled = tb.Text != string.Empty;
        }
    }

    public string Room { get; private set; } = string.Empty;
}
