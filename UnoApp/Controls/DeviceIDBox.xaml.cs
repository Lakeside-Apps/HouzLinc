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

namespace UnoApp.Controls;

sealed partial class DeviceIDBox : UserControl, INotifyPropertyChanged
{
    public DeviceIDBox()
    {
        this.InitializeComponent();
    }

    // Data bdinding to the UI
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };
    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Is the control read-only
    public bool IsReadOnly
    {
        get => isReadOnly;
        set { isReadOnly = value; OnPropertyChanged(); }
    }
    private bool isReadOnly = false;

    /// <summary>
    /// Value of the control
    /// </summary>
    public InsteonID? Value
    {
        get => GetValue(ValueProperty) as InsteonID;
        set => SetValue(ValueProperty, value);
    }

    // We use the callback to handle the change instead of the setter because of
    // https://github.com/unoplatform/uno/issues/9886
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DeviceIDBox idBox)
        {
            idBox.OnPropertyChanged(nameof(Value));
            idBox.OnPropertyChanged(nameof(DeviceIDText));
            idBox.IsValueValid = e.NewValue != null && e.NewValue is InsteonID id && !id.IsNull;
        }
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(InsteonID), typeof(DeviceIDBox), 
            new PropertyMetadata(DependencyProperty.UnsetValue, new PropertyChangedCallback(OnValueChanged)));

    /// <summary>
    /// Whether the value of the control is a valid device ID
    /// </summary>
    public bool IsValueValid
    {
        get => (bool)GetValue(IsValueValidProperty);
        set => SetValue(IsValueValidProperty, value);
    }

    // Is the value a valid InsteonID
    // Register the IsValueValid dependency property
    public static readonly DependencyProperty IsValueValidProperty =
        DependencyProperty.Register(nameof(IsValueValid), typeof(bool), typeof(DeviceIDBox),
            new PropertyMetadata(false));

    // Used to bind to the textbox in the XAML of this user control
    public string DeviceIDText => Value?.ToString() ?? string.Empty;

    // Tracks user typing to validate the value
    private void DeviceIdTextChanged(object sender, TextChangedEventArgs e)
    {
        System.Diagnostics.Debug.Assert(sender is TextBox);

        InsteonID? value = null;
        var text = (sender as TextBox)?.Text ?? string.Empty;
        try
        {
            value = new InsteonID(text);
        }
        catch (Exception)
        {
            value = null;
        }

        if (value != Value)
        {
            Value = value;
        }
    }

}
