
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
/// Follow Behaviors
/// </summary>
public enum FollowBehaviorType
{
    None,
    Not,
    On,
    Off
}

/// <summary>
/// KeypadLinc
/// </summary>
[Bindable(true)]
public sealed class KeypadLincViewModel : DeviceViewModel
{
    public KeypadLincViewModel(Device device) : base(device)
    {
    }

    static internal bool IsA(Device d)
    {
        return DeviceKind.GetModelType(d.CategoryId, d.SubCategory) == DeviceKind.ModelType.KeypadLincDimmer ||
               DeviceKind.GetModelType(d.CategoryId, d.SubCategory) == DeviceKind.ModelType.KeypadLincRelay;
    }

    /// <summary>
    /// This is called when navigating to the item list or detail page to pass which channel to activate.
    /// </summary>
    /// <param name="parameter"></param>
    public override void SetNavigationParameter(string parameter)
    {
        int channelId = int.Parse(parameter);
        if (channelId >= 1 && channelId <= ButtonCount)
        {
            SetDepressedButton(channelId, true);
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
    /// Notifies this KeypadLinc view model that it is about to be made active
    /// (presented on screen) or inactive (hidden from screen)
    /// Also called when the connected state changes (IsConnected)
    /// </summary>
    public override void ActiveStateChanged()
    {
        base.ActiveStateChanged();

        // If we don't have a current channel yet, set the first one as current
        if (depressedButton == 0 && IsActive)
        {
            SetDepressedButton(1, true);
        }
    }

    // -----------------------------------------------------------------------------------
    // Operating Flags
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Operating flags bits - Load Is8Button
    /// Read/write (two way bindable), synced with the physical device
    /// </summary>
    public bool Is8Button
    {
        get => Device.Is8Button;
        set
        {
            Debug.Assert(nameof(Is8Button) == nameof(Device.Is8Button));
            Device.Is8Button = value;
        }
    }

    private int ButtonCount
    {
        get => Is8Button ? 8 : 6;
    }

    /// <summary>
    /// Operating flags bits - KeyBeep
    /// Read/write (two way bindable), synced with the physical device
    /// </summary>
    public bool KeyBeep
    {
        get => Device.KeyBeep;
        set
        {
            Debug.Assert(nameof(KeyBeep) == nameof(Device.KeyBeep));
            Device.KeyBeep = value;
        }
    }

    /// <summary>
    /// Operating flags bits (second byte) - DetachedLoad
    /// Read/write (two way bindable), synced with the physical device
    /// </summary>
    public bool DetachedLoad
    {
        get => Device.DetachedLoad;
        set
        {
            Debug.Assert(nameof(DetachedLoad) == nameof(Device.DetachedLoad));
            Device.DetachedLoad = value;
        }
    }

    /// <summary>
    /// Number of buttons on this keypad, according to the SubCat
    /// </summary>
    public bool Is8ButtonKeypadDevCat => Keypad8SubCats.Contains(this.Device.SubCategory);
    static private int[] Keypad8SubCats = { 0x41 };
    public bool Is6ButtonKeypadDevCat => !Is8ButtonKeypadDevCat;

    /// <summary>
    /// DataTemplate to use when presenting the device details
    /// </summary>
    public override string DeviceTemplateName => $"Keypad{ButtonCount}View";

    /// <summary>
    /// Return a string describing the type of channels on this device
    /// </summary>
    public override string ChannelType => "Button";

    /// <summary>
    /// Return the current channel id (button number), 0 if no current channel
    /// </summary>
    public override int CurrentChannelId
    {
        get
        {
            int channel = depressedButton;

            if (channel == 2 && ButtonCount == 6)
            {
                channel = 1;
            }

            return channel;
        }
    }

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
            OnPropertyChanged("Button" + (sender as ChannelViewModel)?.Id + "Text");
        }
        if (e.PropertyName == "ArePropertiesSynced")
        {
            OnPropertyChanged(nameof(AreChannelsPropertiesSynced));
            OnPropertyChanged(nameof(IsChannelsPropertiesSyncNeeded));
        }
    }

    // -----------------------------------------------------------------------------------
    // Button selection control
    // -----------------------------------------------------------------------------------

    /// <summary>
    /// Button text
    /// </summary>
    public string Button1Text { get => GetChannelName(1); }
    public string Button2Text { get => GetChannelName(2); }
    public string Button3Text { get => GetChannelName(3); }
    public string Button4Text { get => GetChannelName(4); }
    public string Button5Text { get => GetChannelName(5); }
    public string Button6Text { get => GetChannelName(6); }
    public string Button7Text { get => GetChannelName(7); }
    public string Button8Text { get => GetChannelName(8); }

    /// <summary>
    /// Button state
    /// Act as a group of radio buttons
    /// For 6-button keypads, button #2 is button #1 off
    /// </summary>
    public bool Button1Depressed { get => depressedButton == 1; set => SetDepressedButton(1, value); }
    public bool Button2Depressed { get => depressedButton == 2; set => SetDepressedButton(2, value); }
    public bool Button3Depressed { get => depressedButton == 3; set => SetDepressedButton(3, value); }
    public bool Button4Depressed { get => depressedButton == 4; set => SetDepressedButton(4, value); }
    public bool Button5Depressed { get => depressedButton == 5; set => SetDepressedButton(5, value); }
    public bool Button6Depressed { get => depressedButton == 6; set => SetDepressedButton(6, value); }
    public bool Button7Depressed { get => depressedButton == 7; set => SetDepressedButton(7, value); }
    public bool Button8Depressed { get => depressedButton == 8; set => SetDepressedButton(8, value); }

    private int depressedButton = 0;

    private void SetDepressedButton(int button, bool isChecked)
    {
        Debug.Assert(button >= 1 || button <= ButtonCount);

        if (!isSetFollowBehaviorMode)
        {
            int oldDepressedButton = depressedButton;
            bool hasStateChanged = false;

            ChannelViewModel oldActiveChannelViewModel = CurrentChannelViewModel;

            if (!isChecked && button == depressedButton)
            {
                // Toggling currently button off
                depressedButton = 0;
                hasStateChanged = true;
            }
            else if (isChecked && button != depressedButton)
            {
                depressedButton = button;
                hasStateChanged = true;
            }

            if (hasStateChanged)
            {
                OnPropertyChanged(nameof(CurrentChannelId));
                OnPropertyChanged(nameof(CurrentChannelName));
                OnPropertyChanged(nameof(QuotedCurrentChannelName));

                // Send property change notification for "ButtonXDepressed" of the old depressed button
                if (oldDepressedButton >= 0 && oldDepressedButton <= ButtonCount)
                {
                    OnPropertyChanged("Button" + oldDepressedButton + "Depressed");
                }

                // Send property change notification for "ButtonXDepressed" of the new button
                if (depressedButton > 0)
                {
                    OnPropertyChanged("Button" + depressedButton + "Depressed");
                }

                // Send property change notification for CurrentChannel and related properties
                OnCurrentChannelChanged(oldActiveChannelViewModel);

                // Send property change notification for following behavior on all buttons
                for (int b = 1; b <= ButtonCount; b++)
                {
                    OnPropertyChanged("Button" + b + "FollowBehavior");
                }

                // Rebuild the list of scenes this device/channel belongs to
                RebuildScenesUsingThis();

                // Rebuild the lists of link view models for this device's new current channel
                ScheduleRebuildLinksIfActive();
            }
        }
    }

    /// <summary>
    /// Following buttons
    /// Returns whether a button is following the depressed one or not
    /// For 6-button keypads, button #2 is button #1 off
    /// </summary>
    public FollowBehaviorType Button1FollowBehavior { get => GetFollowBehavior(1); set => SetFollowBehavior(1, value); }
    public FollowBehaviorType Button2FollowBehavior { get => GetFollowBehavior(2); set => SetFollowBehavior(2, value); }
    public FollowBehaviorType Button3FollowBehavior { get => GetFollowBehavior(3); set => SetFollowBehavior(3, value); }
    public FollowBehaviorType Button4FollowBehavior { get => GetFollowBehavior(4); set => SetFollowBehavior(4, value); }
    public FollowBehaviorType Button5FollowBehavior { get => GetFollowBehavior(5); set => SetFollowBehavior(5, value); }
    public FollowBehaviorType Button6FollowBehavior { get => GetFollowBehavior(6); set => SetFollowBehavior(6, value); }
    public FollowBehaviorType Button7FollowBehavior { get => GetFollowBehavior(7); set => SetFollowBehavior(7, value); }
    public FollowBehaviorType Button8FollowBehavior { get => GetFollowBehavior(8); set => SetFollowBehavior(8, value); }

    private FollowBehaviorType GetFollowBehavior(int button)
    {
        Debug.Assert(button >= 1 || button <= ButtonCount);

        if (depressedButton > 0)
        {
            // In a 6 keypad 1 and 2 follow each other as on/off buttons
            if (Is6ButtonKeypadDevCat && ((depressedButton == 1 && button == 2) || (depressedButton == 2 && button == 1)))
            {
                return FollowBehaviorType.Off;
            }

            if ((CurrentChannelViewModel.FollowMask & (1 << (button - 1))) != 0)
            {
                if ((CurrentChannelViewModel.FollowOffMask & (1 << (button - 1))) != 0)
                {
                    return FollowBehaviorType.Off;
                }
                else
                {
                    return FollowBehaviorType.On;

                }
            }
            else
            {
                return FollowBehaviorType.Not;
            }
        }
        else
        {
            return FollowBehaviorType.None;
        }
    }

    private void SetFollowBehavior(int button, FollowBehaviorType behavior)
    {
        Debug.Assert(button >= 1 || button <= ButtonCount);

        if (GetFollowBehavior(button) == behavior)
            return;

        if (button != depressedButton)
        {
            switch (behavior)
            {
                case FollowBehaviorType.None:
                    {
                        break;
                    }
                case FollowBehaviorType.Not:
                    {
                        CurrentChannelViewModel.FollowMask &= ~(1 << (button - 1));
                        CurrentChannelViewModel.FollowOffMask &= ~(1 << (button - 1));
                        break;
                    }
                case FollowBehaviorType.Off:
                    {
                        CurrentChannelViewModel.FollowMask |= (1 << (button - 1));
                        CurrentChannelViewModel.FollowOffMask |= (1 << (button - 1));
                        break;
                    }
                case FollowBehaviorType.On:
                    {
                        CurrentChannelViewModel.FollowMask |= (1 << (button - 1));
                        CurrentChannelViewModel.FollowOffMask &= ~(1 << (button - 1));
                        break;
                    }
            }
            OnPropertyChanged($"Button{button}FollowBehavior");
            SetFollowBehaviorHelpText(button);
            CurrentChannelViewModel.OnIsLoadControllingChanged();
        }
    }

    /// <summary>
    /// Compute the help text for the follow behavior of a button
    /// </summary>
    public string FollowBehaviorHelpText
    {
        get => followBehaviorType;
        set
        {
            if (value != followBehaviorType)
            {
                followBehaviorType = value;
                OnPropertyChanged();
            }

        }
    }
    private string followBehaviorType;

    // Helper to set the followBehaviorText bindable variable
    // and show the follow behavior help message
    private void SetFollowBehaviorHelpText(int button)
    {
        var behavior = GetFollowBehavior(button);
        if (behavior == FollowBehaviorType.None)
        {
            FollowBehaviorHelpText = string.Empty;
        }
        else if (behavior == FollowBehaviorType.Not)
        {
            FollowBehaviorHelpText = $"Button '{GetChannelName(button)}' doesn't follow '{CurrentChannelName}'";
        }
        else if (behavior == FollowBehaviorType.Off)
        {
            FollowBehaviorHelpText = $"Button '{GetChannelName(button)}' follows '{CurrentChannelName}' Off";
        }
        else if (behavior == FollowBehaviorType.On)
        {
            FollowBehaviorHelpText = $"Button '{GetChannelName(button)}' follows '{CurrentChannelName}' On";
        }

        // Turn off help message after 5 seconds
        var timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(5);
        timer.Tick += (sender, e) =>
        {
            timer.Stop();
            FollowBehaviorHelpText = string.Empty;
        };
        timer.Start();
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
            return allLinkRecord.Group == depressedButton;
        }
        else
        {
            // Responder links to the hub can have data3 == 0
            // show them on the first 
            return allLinkRecord.Data3 == depressedButton;
        }
    }

    /// <summary>
    /// Filter the list of scenes using this device to those using the current button
    /// </summary>
    /// <param name="sceneMember"></param>
    /// <returns>Returns true to include</returns>
    protected override bool FilterScenes(SceneMember sceneMember)
    {
        return depressedButton == 0 || sceneMember.Group == depressedButton;
    }

    /// <summary>
    /// Are channel properties synced for all channels
    /// OnPropertychanged is fired by OnChannelPropertyChanged
    /// </summary>
    public bool AreChannelsPropertiesSynced
    {
        get
        {
            for (int i = 0; i < Device.ChannelCount; i++)
            {
                if (!ChannelViewModels[i].ArePropertiesSynced)
                {
                    return false;
                }
            }
            return true;
        }
    }
    public bool IsChannelsPropertiesSyncNeeded => !AreChannelsPropertiesSynced;

    /// <summary>
    /// Pin Mode allows to change following buttons of the current (pinned) button
    /// </summary>
    public bool IsSetFollowBehaviorMode
    {
        get => isSetFollowBehaviorMode;
        set
        {
            if (value != isSetFollowBehaviorMode)
            {
                isSetFollowBehaviorMode = value;

                // Send property change notification to all buttons
                for (int b = 1; b <= ButtonCount; b++)
                {
                    OnPropertyChanged("IsButton" + b + "InSetFollowBehaviorMode");
                }
            }
        }
    }
    private bool isSetFollowBehaviorMode;

    public bool IsButton1InSetFollowBehaviorMode => IsSetFollowBehaviorMode && depressedButton != 1;
    public bool IsButton2InSetFollowBehaviorMode => IsSetFollowBehaviorMode && depressedButton != 2;
    public bool IsButton3InSetFollowBehaviorMode => IsSetFollowBehaviorMode && depressedButton != 3;
    public bool IsButton4InSetFollowBehaviorMode => IsSetFollowBehaviorMode && depressedButton != 4;
    public bool IsButton5InSetFollowBehaviorMode => IsSetFollowBehaviorMode && depressedButton != 5;
    public bool IsButton6InSetFollowBehaviorMode => IsSetFollowBehaviorMode && depressedButton != 6;
    public bool IsButton7InSetFollowBehaviorMode => IsSetFollowBehaviorMode && depressedButton != 7;
    public bool IsButton8InSetFollowBehaviorMode => IsSetFollowBehaviorMode && depressedButton != 8;

    /// <summary>
    /// Whether the current button controls the load, either directly or through follow behavior.
    /// </summary>
    public bool IsCurrentButtonLoadControlling => depressedButton == 1 || GetFollowBehavior(1) == FollowBehaviorType.On;
}
