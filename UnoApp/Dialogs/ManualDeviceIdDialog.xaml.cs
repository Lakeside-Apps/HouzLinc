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

namespace HouzLinc.Dialogs;

public sealed partial class ManualDeviceIdDialog : ContentDialog
{
    public ManualDeviceIdDialog(XamlRoot? xamlRoot)
    {
        this.InitializeComponent();
        this.XamlRoot = xamlRoot;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        DeviceId = DeviceIdBox.Value;
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }

    /// <summary>
    /// User manually entered device id
    /// </summary>
    public InsteonID? DeviceId;
}
