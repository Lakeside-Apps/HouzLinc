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

using System.Diagnostics;
using Insteon.Model;
using Insteon.Base;
using System.ComponentModel;

namespace ViewModel.Devices;

/// <summary>
/// RemoteLinc and mini-remote (Cat: 0x00, Subcats: 0x05, 0x0E, 0x10, etc.)
/// </summary>
[Bindable(true)]
public sealed class RemoteLincViewModel : DeviceViewModel
{
    public RemoteLincViewModel(Device d) : base(d)
    {
    }

    static internal bool IsA(Device d)
    {
        return DeviceKind.GetModelType(d.CategoryId, d.SubCategory) == DeviceKind.ModelType.RemoteLinc;
    }

    /// <summary>
    /// This is called when navigating to the item list or detail page to pass which channel to activate.
    /// </summary>
    /// <param name="parameter"></param>
    public override void SetNavigationParameter(string parameter)
    {
        int channelId = int.Parse(parameter);
        if (channelId >= 1 && channelId <= channelCount)
        {
            SelectChannel(channelId, true);
        }
    }

    /// <summary>
    /// This is called when navigating from the item list or detail page to retrieve which channel to activate
    /// when navigating back to this item.
    /// </summary>
    /// <returns>The navigation parameter, null if none</returns>
    public override string? GetNavigationParameter()
    {
        if (HasChannels)
        {
            return CurrentChannelId.ToString();
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Notifies this RemoteLinc device view model that it is about to be made active
    /// (presented on screen) or inactive (hidden from screen)
    /// Also called when the connected state changes (IsConnected)
    /// </summary>
    public override void ActiveStateChanged()
    {
        base.ActiveStateChanged();

        // If we don't have a current channel yet, set the first one as current
        if (selectedChannel == 0 && IsActive)
        {
            SelectChannel(1, true);
        }
    }

    // -----------------------------------------------------------------------------------
    // Remotelinc Property bag properties
    // -----------------------------------------------------------------------------------
    /// <summary>
    /// Snoozed
    /// Do not ask the user to wake up the device
    /// </summary>
    public bool Snoozed
    {
        get { return Device.Snoozed; }
        set
        {
            // See above
            Debug.Assert(nameof(Snoozed) == nameof(Device.Snoozed));
            Device.Snoozed = (bool)value;
        }
    }

    // -----------------------------------------------------------------------------------
    // Operating Flags
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Operating flags bits - LEDOn
    /// Read/write (two way bindable), synced with the physical device
    /// </summary>
    public override bool LEDOn
    {
        get { return Device.LEDOnTx; }
        set
        {
            Debug.Assert(nameof(LEDOn) == nameof(Device.LEDOnTx));
            Device.LEDOnTx = value;
        }
    }

    /// <summary>
    /// Operating flags bits - BeeperOn
    /// Read/write (two way bindable), synced with the physical device
    /// </summary>
    public bool BeeperOn
    {
        get { return Device.BeeperOn; }
        set
        {
            Debug.Assert(nameof(BeeperOn) == nameof(Device.BeeperOn));
            Device.BeeperOn = value;
        }
    }

    /// <summary>
    /// Operating flags bits - Allow Sleep
    /// Read/write (two way bindable), synced with the physical device
    /// </summary>
    public bool StayAwake
    {
        get { return Device.StayAwake; }
        set
        {
            Debug.Assert(nameof(StayAwake) == nameof(Device.StayAwake));
            Device.StayAwake = value;
        }
    }

    /// <summary>
    /// Operating flags bits - Receive Only
    /// Read/write (two way bindable), synced with the physical device
    /// </summary>
    public bool ReceiveOnly
    {
        get { return Device.ReceiveOnly; }
        set
        {
            Debug.Assert(nameof(ReceiveOnly) == nameof(Device.ReceiveOnly));
            Device.ReceiveOnly = value;
        }
    }

    /// <summary>
    /// Operating flags bits - NoHeartbeat
    /// Read/write (two way bindable), synced with the physical device
    /// </summary>
    public bool NoHeartbeat
    {
        get { return Device.NoHeartbeat; }
        set
        {
            Debug.Assert(nameof(NoHeartbeat) == nameof(Device.NoHeartbeat));
            Device.NoHeartbeat = value;
        }
    }

    // Override OnDevicePropertyChanged to handle the LEDOnTx property
    protected override void OnDevicePropertyChanged(string propertyName)
    {
        if (propertyName == nameof(Device.LEDOnTx))
            propertyName = nameof(LEDOn);
        base.OnDevicePropertyChanged(propertyName);
    }

    /// <summary>
    /// DataTemplate to use when presenting the device details
    /// </summary>
    public override string DeviceTemplateName => $"Remote{channelCount}View";

    // -----------------------------------------------------------------------------------
    // Channel properties
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Return a string describing the type of channels on this device
    /// </summary>
    public override string ChannelType => "Channel";

    private int channelCount => Device.ChannelCount;

    /// <summary>
    /// Return the current channel id (button number)
    /// </summary>
    public override int CurrentChannelId => selectedChannel;

    /// <summary>
    /// Registers a new channel for this device
    /// Gives this class an opportunity to add property listeners on the channel
    /// </summary>
    /// <param name="channelViewModel">new channel to register</param>
    private protected override void RegisterChannel(ChannelViewModel channelViewModel)
    {
        channelViewModel.PropertyChanged += OnChannelPropertyChanged;
    }

    // Listens to property changes from the channels and forward as appropriate
    private void OnChannelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.Assert(sender is ChannelViewModel);
        if (e.PropertyName == "Name")
        {
            OnPropertyChanged("Channel" + (sender as ChannelViewModel)?.Id + "OnText");
        }
    }

    // -----------------------------------------------------------------------------------
    // Channel selection control
    // -----------------------------------------------------------------------------------

    // Compute the selected buttons from the selected channel for 4 and 8 channels remote lincs
    private int ChannelFromButton(int button) => channelCount == 4 ? (button + 1) / 2 : button;
    //private int ButtonFromChannel(int button) => channelCount == 4 ? (button + 1) / 2 : button;

    /// <summary>
    /// Channel selection state
    /// Act as a group of radio buttons
    /// For 4 scene/channel remotelincs, only the first 4 channels are used
    /// </summary>
    public bool Button1Checked { get { return selectedChannel == ChannelFromButton(1); } set { SelectChannel(ChannelFromButton(1), value); } }
    public bool Button2Checked { get { return selectedChannel == ChannelFromButton(2); } set { SelectChannel(ChannelFromButton(2), value); } }
    public bool Button3Checked { get { return selectedChannel == ChannelFromButton(3); } set { SelectChannel(ChannelFromButton(3), value); } }
    public bool Button4Checked { get { return selectedChannel == ChannelFromButton(4); } set { SelectChannel(ChannelFromButton(4), value); } }
    public bool Button5Checked { get { return selectedChannel == ChannelFromButton(5); } set { SelectChannel(ChannelFromButton(5), value); } }
    public bool Button6Checked { get { return selectedChannel == ChannelFromButton(6); } set { SelectChannel(ChannelFromButton(6), value); } }
    public bool Button7Checked { get { return selectedChannel == ChannelFromButton(7); } set { SelectChannel(ChannelFromButton(7), value); } }
    public bool Button8Checked { get { return selectedChannel == ChannelFromButton(8); } set { SelectChannel(ChannelFromButton(8), value); } }

    private int selectedChannel = 0;

    // Handle selecting/deselecting a new channel
    private void SelectChannel(int channel, bool isSelected)
    {
        Debug.Assert(channel >= 1 && channel <= channelCount);

        int oldSelectedChannel = selectedChannel;
        bool hasStateChanged = false;

        ChannelViewModel oldActiveChannelViewModel = CurrentChannelViewModel;

        if (!isSelected && channel == selectedChannel)
        {
            // Toggling currently button off
            selectedChannel = 0;
            hasStateChanged = true;
        }
        else if (isSelected && channel != selectedChannel)
        {
            selectedChannel = channel;
            hasStateChanged = true;
        }

        if (hasStateChanged)
        {
            OnPropertyChanged(nameof(CurrentChannelId));
            OnPropertyChanged(nameof(CurrentChannelName));
            OnPropertyChanged(nameof(QuotedCurrentChannelName));

            // Send property change notification for "ButtonXSelected" with the old selected channel
            if (oldSelectedChannel >= 0 && oldSelectedChannel <= channelCount)
            {
                if (channelCount == 4)
                {
                    OnPropertyChanged("Button" + (oldSelectedChannel * 2 - 1) + "Checked");
                    OnPropertyChanged("Button" + (oldSelectedChannel * 2) + "Checked");
                }
                else
                {
                    OnPropertyChanged("Button" + oldSelectedChannel + "Checked");
                }
            }

            // Send property change notification for "ButtonXSelected" with the new selected channel
            if (selectedChannel > 0)
            {
                if (channelCount == 4)
                {
                    OnPropertyChanged("Button" + (selectedChannel * 2 - 1) + "Checked");
                    OnPropertyChanged("Button" + (selectedChannel * 2) + "Checked");
                }
                else
                {
                    OnPropertyChanged("Button" + selectedChannel + "Checked");
                }
            }

            // Send property change notification for CurrentChannel and related properties
            OnCurrentChannelChanged(oldActiveChannelViewModel);

            // Rebuild the list of scenes this device/channel belongs to
            RebuildScenesUsingThis();

            // Rebuild the lists of link view models for this device's new current channel
            ScheduleRebuildLinksIfActive();
        }
    }

    /// <summary>
    /// Filter the list of links to the current channel (button)
    /// </summary>
    /// <param name="allLinkRecord"></param>
    /// <returns>Returns true to include</returns>
    protected override bool FilterLink(AllLinkRecord allLinkRecord)
    {
        if (allLinkRecord.IsController)
        {
            return allLinkRecord.Group == selectedChannel;
        }
        else
        {
            // Responder links to the hub can have data3 == 0
            // show them on the first 
            return allLinkRecord.Data3 == selectedChannel;
        }
    }

    /// <summary>
    /// Filter the list of scenes using this device to those using the current button
    /// </summary>
    /// <param name="sceneMember"></param>
    /// <returns>Returns true to include</returns>
    protected override bool FilterScenes(SceneMember sceneMember)
    {
        return selectedChannel == 0 || sceneMember.Group == selectedChannel;
    }
}
