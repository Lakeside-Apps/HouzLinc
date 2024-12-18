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
using Common;

namespace ViewModel.Base;

public class BaseViewModel : ObservableObject
{
    /// <summary>
    /// Whether this view is under the mouse
    /// Bindable, one-way
    /// </summary>
    public bool IsPointerOver
    {
        get => isPointerOver;
        set
        {
            if (isPointerOver != value)
            {
                isPointerOver = value;
                OnPropertyChanged();
                PointerOverChanged();
            }
        }
    }
    private bool isPointerOver;

    // Can be override by derived classes to be notified when the mouse is over the view
    public virtual void PointerOverChanged() { }

    /// <summary>
    /// Pointer event handlers to create IsMouseOver
    /// Virtual because derived classes have to override or get Intellisense error in XAML 
    /// where these handler are referenced
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public virtual void PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        IsPointerOver = true;
    }

    public virtual void PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        IsPointerOver = false;
    }
}
