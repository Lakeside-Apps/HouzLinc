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

using UnoApp.Utils;

namespace UnoApp.Views.Hub;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HubChannelView : ContentControl
{
    public HubChannelView()
    {
        this.InitializeComponent();
    }

    private void FocusNameBox(object sender, RoutedEventArgs e)
    {
        if (sender is DependencyObject de)
        {
            XAMLHelpers.FocusEditableTextBlock(de, "ChannelNameBox");
        }
    }
}
