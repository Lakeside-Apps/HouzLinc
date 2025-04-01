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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UnoApp.Controls;

/// <summary>
/// A ContentControl to hold the status bar which displays log messages
/// And provides buttons to save and sync the house config (temporary)
/// </summary>
public sealed partial class StatusBar : ContentControl, INotifyPropertyChanged
{
    public StatusBar()
    {
        this.InitializeComponent();

        // Listen to viewmodel property changes
        SettingsViewModel.Instance.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
    }

    /// <summary>
    /// Data binding support
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        // Raise the PropertyChanged event, passing the name of the property whose value has changed.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Text to show in the status bar
    /// </summary>
    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }
    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(StatusBar), new PropertyMetadata(""));

    /// <summary>
    /// Whether the status bar is showing a user action request,
    /// i.e., with confirm and decline buttons
    /// </summary>
    public bool IsUserActionRequest
    {
        get => (bool)GetValue(IsUserActionRequestProperty);
        set => SetValue(IsUserActionRequestProperty, value);
    }
    public static readonly DependencyProperty IsUserActionRequestProperty =
        DependencyProperty.Register(nameof(IsUserActionRequest), typeof(bool), typeof(StatusBar), new PropertyMetadata(false));

    private SettingsViewModel settingsViewModel = SettingsViewModel.Instance;

    private void ConfirmUserAction(object sender, RoutedEventArgs e)
    {
        Holder.ConfirmUserAction();
    }

    private void DeclineUserAction(object sender, RoutedEventArgs e)
    {
        Holder.DeclineUserAction();
    }
}
