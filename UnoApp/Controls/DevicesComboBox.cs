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
using ViewModel.Devices;
using ViewModel.Base;
using ViewModel.Settings;


namespace UnoApp.Controls;

/// <summary>
/// Combobox to select a device
/// </summary>
public partial class DevicesComboBox : ComboBox
{
    public DevicesComboBox()
    {
        RecreateDeviceList();
        DisplayMemberPath = nameof(DeviceViewModel.LocationDisplayNameAndId); ;
        SelectionChanged += OnSelectedItemChanged;
    }

    /// <summary>
    /// Whether to include the hub (or hubs) in the list of devices or not
    /// </summary>
    public bool IncludeHub
    {
        get => includeHub;
        set
        {
            if (includeHub != value)
            {
                includeHub = value;
                RecreateDeviceList();
            }
        }
    }
    private bool includeHub;

    private void RecreateDeviceList()
    {
        DeviceListViewModel dlvm;
        // TODO: this won't work if we have have multiple house configs
        dlvm = DeviceListViewModel.Create(Holder.House.Devices, includeHub);
        dlvm.SortByRoom(SortDirection.Ascending);
        ItemsSource = dlvm.Items;
    }

    /// <summary>
    /// ID of the Currently selected device
    /// </summary>
    public InsteonID SelectedDeviceID
    {
        get => (InsteonID)GetValue(SelectedDeviceIDProperty);
        set => SetValue(SelectedDeviceIDProperty, value);
    }

    // Sets the SelectedDeviceID property to reflect a selection change in the control
    private void OnSelectedItemChanged(object sender, SelectionChangedEventArgs args)
    {
        if (SelectedItem != null && SelectedItem is DeviceViewModel selectedItem)
        {
            SelectedDeviceID = selectedItem.Id;
        }
    }

    private static void OnSelectedDeviceIDChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DevicesComboBox thisComboBox)
        {
            if (e.NewValue != null)
            {
                if (e.NewValue is InsteonID newValue && newValue != (e.OldValue as InsteonID))
                {
                    thisComboBox.SelectedItem = DeviceViewModel.GetOrCreateById(Holder.House, newValue);
                }
            }
            else
            {
                thisComboBox.SelectedItem = null;
            }
        }
    }

    public static readonly DependencyProperty SelectedDeviceIDProperty =
        DependencyProperty.Register("SelectedDeviceID", typeof(InsteonID), typeof(DevicesComboBox), 
            new PropertyMetadata(null, new PropertyChangedCallback(OnSelectedDeviceIDChanged)));
}
