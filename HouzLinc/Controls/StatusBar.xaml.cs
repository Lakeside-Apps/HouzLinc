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

namespace HouzLinc.Controls;

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

    /// <summary>
    /// One way bindable to UI to indicate the house config needs saving
    /// TODO: this is temporary to allow user control over saving to model file
    /// Should be automatic in the future
    /// </summary>
    public bool DoesHouseConfigNeedSave => SettingsViewModel.Instance.DoesHouseConfigNeedSave;

    /// <summary>
    /// One way bindable to UI to indicate the house config needs syncing
    /// TODO: this is temporary to allow user control over the sync process
    /// Should be automatic in the future
    /// </summary>
    public bool DoesHouseConfigNeedSync => SettingsViewModel.Instance.DoesHouseConfigNeedSync;

    /// <summary>
    /// One way bindable to UI to indicate we are pushing traffic through the gateway
    /// </summary>
    public bool HasGatewayTraffic => SettingsViewModel.Instance.HasGatewayTraffic;

    // Property changed notifications for properties above
    private void OnViewModelPropertyChanged(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(SettingsViewModel.DoesHouseConfigNeedSave):
                OnPropertyChanged(nameof(DoesHouseConfigNeedSave));
                break;

            case nameof(SettingsViewModel.DoesHouseConfigNeedSync):
                OnPropertyChanged(nameof(DoesHouseConfigNeedSync));
                break;

            case nameof(SettingsViewModel.HasGatewayTraffic):
                OnPropertyChanged(nameof(HasGatewayTraffic));
                break;
        }
    }

    // Actions from the buttons in the status bar.
    // TODO: these should really be events fired by this StatusBar control
    // and handled by the parent page
    private async void SaveHouseConfig(object sender, RoutedEventArgs e)
    {
        await SettingsViewModel.Instance.SaveHouse();
    }

    private void SyncHouseConfig(object sender, RoutedEventArgs e)
    {
        SettingsViewModel.Instance.SyncHouse();
    }

    private void ConfirmUserAction(object sender, RoutedEventArgs e)
    {
        Holder.ConfirmUserAction();
    }

    private void DeclineUserAction(object sender, RoutedEventArgs e)
    {
        Holder.DeclineUserAction();
    }
}
