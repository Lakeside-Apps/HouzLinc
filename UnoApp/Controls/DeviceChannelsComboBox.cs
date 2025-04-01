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
using ViewModel.Settings;

namespace UnoApp.Controls;

public partial class DeviceChannelsComboBox : ComboBox
{
    public DeviceChannelsComboBox()
    {
        SelectionChanged += OnSelectedItemChanged;
    }

    /// <summary>
    /// Insteon Id of the device this channel combobox control is for
    /// </summary>
    public InsteonID DeviceId
    {
        get => deviceViewModel?.Id ?? InsteonID.Null;
        set
        {
            if ((deviceViewModel == null || value != deviceViewModel.Id))
            {
                if (value != null && !value.IsNull)
                {
                    deviceViewModel = DeviceViewModel.GetOrCreateById(Holder.House, value);
                }
                else
                {
                    deviceViewModel = null;
                }

                if (deviceViewModel != null)
                {
                    ItemsSource = deviceViewModel.ChannelViewModels;
                    DisplayMemberPath = "IdAndName";
                }
                else
                {
                    // If we don't have a device viewmodel (e.g., this was called for 00.00.00)
                    // show a list of channels with a single blank item
                    List<string> items = new List<string>();
                    items.Add("");
                    ItemsSource = items;
                    ChannelId = 1;
                }

                // Setting ItemsSource reset the selected index to -1
                if (Items.Count > 0)
                {
                    OnChannelIdChanged(deviceViewModel);
                }
            }
        }
    }

    /// <summary>
    /// Id of the Currently selected channel
    /// </summary>
    public int ChannelId
    {
        get => (int)GetValue(ChannelIdProperty);
        set => SetValue(ChannelIdProperty, value);
    }

    // Sets the ChannelId property to reflect a selection change in the control
    private void OnSelectedItemChanged(object sender, SelectionChangedEventArgs args)
    {
        if (SelectedItem != null)
        {
            // Channel Ids are 1 based for KeypadLincs, 0 based for the hub
            if (deviceViewModel != null && deviceViewModel.IsHub)
            {
                ChannelId = SelectedIndex;
                System.Diagnostics.Debug.Assert(ChannelId >= 0 && ChannelId < Items.Count);
            }
            else
            {
                ChannelId = SelectedIndex + 1;
                System.Diagnostics.Debug.Assert(ChannelId > 0 && ChannelId <= Items.Count);
            }

            // For 6 button keypads, channel 2 does not exist (it's the off button for channel 1)
            if (deviceViewModel != null && deviceViewModel is KeypadLincViewModel klvm)
            {
                if (ChannelId == 2 && deviceViewModel.IsKeypadLinc && !klvm.Is8Button)
                {
                    ChannelId = 1;
                }
            }
        }
    }

    private void OnChannelIdChanged(DeviceViewModel? deviceViewModel)
    {
        if (Items.Count > 0)
        {
            // channel Ids are 1 based for KeypadLincs, 0 based for the hub
            if (deviceViewModel != null && deviceViewModel.Device.FirstChannelId == 0)
            {
                // ChannelId is 0 based
                System.Diagnostics.Debug.Assert(ChannelId >= 0 && ChannelId < Items.Count);
                SelectedIndex = ChannelId;
            }
            else
            {
                // ChannelId is 1 based, 0 means no channel selected
                System.Diagnostics.Debug.Assert(ChannelId >= 0 && ChannelId <= Items.Count);
                SelectedIndex = ChannelId - 1;
            }
        }
    }

    private static void OnChannelIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if ((int)e.NewValue != (int)e.OldValue)
        {
            if (d is DeviceChannelsComboBox dccb)
            {
                System.Diagnostics.Debug.Assert((int)e.NewValue == dccb.ChannelId);
                dccb.OnChannelIdChanged(dccb.deviceViewModel);
            }
        }
    }

    public static readonly DependencyProperty ChannelIdProperty =
        DependencyProperty.Register(nameof(ChannelId), typeof(int), typeof(DeviceChannelsComboBox), 
            new PropertyMetadata(0, new PropertyChangedCallback(OnChannelIdChanged)));

    DeviceViewModel? deviceViewModel;
}
