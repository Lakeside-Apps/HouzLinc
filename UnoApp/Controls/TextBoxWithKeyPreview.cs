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

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace UnoApp.Controls;

public partial class TextBoxWithKeyPreview : TextBox
{
    public TextBoxWithKeyPreview()
    {
    }

    protected override void OnKeyUp(KeyRoutedEventArgs e)
    {
        CustomPreviewKeyUp?.Invoke(this, e);
        if (!e.Handled)
        {
            base.OnKeyUp(e);
        }
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        CustomPreviewKeyDown?.Invoke(this, e);
        if (!e.Handled)
        {
            base.OnKeyDown(e);
        }
    }

    public event KeyEventHandler? CustomPreviewKeyDown;
    public event KeyEventHandler? CustomPreviewKeyUp;
}
