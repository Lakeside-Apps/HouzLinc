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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UnoApp.Controls
{
    public sealed partial class EditableTextBlock : UserControl, INotifyPropertyChanged
    {
        public EditableTextBlock()
        {
            this.InitializeComponent();
            RegisterPropertyChangedCallback(FontSizeProperty, FontSizeChanged);
        }

        private void FontSizeChanged(DependencyObject sender, DependencyProperty dp)
        {
            EditableTextBox.FontSize = FontSize;
            OnPropertyChanged(nameof(TextBlockPadding));
        }

        // Ajust padding of the read only textblock to match size of the textbox
        public Thickness TextBlockPadding => new Thickness(11, 5, 0, 8);

        // Property binding support
        public event PropertyChangedEventHandler? PropertyChanged = delegate { };
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        // We use the callback to handle the change instead of the setter because of
        // https://github.com/unoplatform/uno/issues/9886
        private static void OnTextPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is EditableTextBlock etb && e.NewValue != e.OldValue)
            {
                etb.OnPropertyChanged();
            }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(EditableTextBlock), 
                new PropertyMetadata("", new PropertyChangedCallback(OnTextPropertyChanged)));

        public bool IsEditable
        {
            get => (bool)GetValue(IsEditableProperty);
            set => SetValue(IsEditableProperty, value);
        }

        // We use the callback to handle the change instead of the setter because of
        // https://github.com/unoplatform/uno/issues/9886
        private static void IsEditableChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is EditableTextBlock etb && e.NewValue != e.OldValue)
            {
                etb.OnPropertyChanged();
            }
        }

        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(EditableTextBlock), 
                new PropertyMetadata(false, new PropertyChangedCallback(IsEditableChanged)));
    
        // Clicking on the readonly form makes it editable
        private void TextBlock_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            IsEditable = true;
        }

        private void TextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                IsEditable = false;
                e.Handled = true;
            }
        }

        public void SelectAll()
        {
            EditableTextBox.SelectAll();
        }
    }
}
