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
using Insteon.Model;
using ViewModel.Base;
using ViewModel.Devices;

namespace ViewModel.Scenes;

/// <summary>
/// Comparer to sort
/// </summary>
public sealed class MemberViewModelComparer : IComparer<MemberViewModel>
{
    public int Compare(MemberViewModel? x, MemberViewModel? y)
    {
        return MemberViewModel.Compare(x, y);
    }
}

/// <summary>
/// Represents the view model for a scene member in a given scene
/// TO handle adding a new member in the UI, this class needs to handle the cases where there is no device given for this member yet
/// </summary>
public sealed class MemberViewModel : CollectionItemViewModel<MemberListViewModel, MemberViewModel>
{
    public MemberViewModel(MemberListViewModel containingList, SceneMember member) : base(containingList)
    {
        deviceId = member.DeviceId;
        group = member.Group;
        isController = member.IsController;
        isResponder = member.IsResponder;
        onLevel = member.Data1;
        rampRate = member.Data2;
    }

    public MemberViewModel(MemberViewModel mvm) : base(mvm.containingList)
    {
        deviceId = mvm.deviceId;
        group = mvm.group;
        isController = mvm.isController;
        isResponder = mvm.isResponder;
        onLevel = mvm.onLevel;
        rampRate = mvm.rampRate;
    }

    public SceneMember Member => new SceneMember(SceneViewModel.Scene)
    {
        DeviceId = DeviceId,
        Group = (byte)Group,
        IsController = IsController,
        IsResponder = IsResponder,
        Data1 = (byte)OnLevel,
        Data2 = (byte)RampRate,
        Data3 = (byte)Group
    };

    public override bool Equals(object? obj)
    {
        return
            obj is MemberViewModel memberViewModel &&
            DeviceId == memberViewModel.DeviceId &&
            Group == memberViewModel.Group &&
            IsController == memberViewModel.IsController &&
            IsResponder == memberViewModel.IsResponder &&
            OnLevel == memberViewModel.OnLevel &&
            RampRate == memberViewModel.RampRate;
    }

    public override int GetHashCode()
    {
        return DeviceId.ToInt();
    }

    public SceneViewModel SceneViewModel => containingList.SceneViewModel;

    /// <summary>
    /// Returns DeviceViewModel of this member device
    /// </summary>
    public DeviceViewModel? DeviceViewModel
    {
        get
        {
            if (deviceViewModel == null && HasDevice)
            {
                deviceViewModel = DeviceViewModel.GetOrCreateById(SceneViewModel.Scene.House, Member.DeviceId);
            }
            return deviceViewModel;
        }
    }
    private DeviceViewModel? deviceViewModel;

    /// <summary>
    /// Member device id (can be null)
    /// </summary>
    public InsteonID DeviceId
    {
        get => deviceId;
        set
        {
            if (deviceId != value)
            {
                deviceId = value;

                // Force acquiring a new DeviceViewModel
                deviceViewModel = null;

                // Reset the group as it might not be in the correct range for the new device
                Group = 0;

                OnPropertyChanged();

                // All device dependent properties have changed
                OnPropertyChanged(nameof(HasDevice));
                OnPropertyChanged(nameof(DeviceModelName));
                OnPropertyChanged(nameof(DeviceDisplayName));
                OnPropertyChanged(nameof(DeviceDisplayNameAndId));
                OnPropertyChanged(nameof(DeviceModelIconPath_64x64));
                OnPropertyChanged(nameof(DeviceHasRoom)); 
                OnPropertyChanged(nameof(DeviceRoom));
                OnPropertyChanged(nameof(DeviceHasLocation));
                OnPropertyChanged(nameof(DeviceLocation));
                OnPropertyChanged(nameof(DeviceHasChannels));
                OnPropertyChanged(nameof(DeviceChannelType));
                OnPropertyChanged(nameof(DeviceChannelHeader));
                OnPropertyChanged(nameof(Group));
                OnPropertyChanged(nameof(OnLevel));
                OnPropertyChanged(nameof(RampRate));
            }
        }
    }
    private InsteonID deviceId;

    // Equality 
    public bool Equals(MemberViewModel other)
    {
        return Compare(this, other) == 0;
    }

    // Compare function to sort on MemberViewModel
    internal static int Compare(MemberViewModel? x, MemberViewModel? y)
    {
        if (x == null || y == null) return 1;
        else if (x.DeviceId < y.DeviceId) return -1;
        else if (x.DeviceId > y.DeviceId) return 1;
        else if (x.Group < y.Group) return -1;
        else if (x.Group > y.Group) return 1;
        else if (x.IsResponder && !y.IsResponder) return -1;
        else if (x.IsController && !y.IsController) return 1;
        else return 0;
    }

    public bool HasDevice => Member.DeviceId != null && !Member.DeviceId.IsNull;
    public string DeviceModelName => DeviceViewModel?.ModelName ?? string.Empty;
    public string DeviceDisplayName => DeviceViewModel?.DisplayName ?? "***Unknow Device***";
    public string DeviceDisplayNameAndId => DeviceViewModel?.DisplayNameAndId ?? "***Unknow Device*** (" + Member.DeviceId + ")";
    public string DeviceModelIconPath_64x64 => DeviceViewModel?.ModelIconPath_72x72 ?? "NotFound.png";
    public string DeviceRoom => DeviceViewModel?.Room ?? string.Empty;
    public string DeviceRoomAndLocation => DeviceRoom + (DeviceHasLocation ? ", " + DeviceLocation : string.Empty) ;
    public bool DeviceHasRoom => DeviceViewModel?.HasRoom ?? false;
    public string DeviceLocation => DeviceViewModel?.Location ?? string.Empty;
    public bool DeviceHasLocation => DeviceViewModel?.HasLocation ?? false;
    public bool DeviceHasChannels => DeviceViewModel?.HasChannels ?? false;
    public string DeviceChannelType => DeviceViewModel?.ChannelType ?? string.Empty;
    
    public string DeviceChannelHeader => DeviceHasChannels ? DeviceChannelType + ":" : "";
    public string DeviceChannelTypeIdAndName => (DeviceHasChannels && DeviceViewModel != null) ?
        ($"{DeviceViewModel.ChannelType} {Group.ToString()} {(DeviceViewModel.GetChannelName(Group) != string.Empty ? "- " + DeviceViewModel.GetChannelName(Group) : string.Empty)}") : string.Empty;


    public int Group
    {
        get => group;
        set
        {
            if (group == value) return;
            group = value;
            OnPropertyChanged();
        }
    }
    private int group;

    public bool IsController
    {
        get => isController;
        set
        {
            if (isController == value) return;
            isController = value;
            OnPropertyChanged();
        }
    }
    private bool isController;

    public bool IsResponder
    {
        get => isResponder;
        set
        {
            if (isResponder == value) return;
            isResponder = value;
            OnPropertyChanged();
        }
    }
    private bool isResponder;

    public bool IsBoth => Member.IsController && Member.IsResponder;

    /// <summary>
    /// Whether this member device support Ramp Rate
    /// </summary>
    public bool HasRampRate => DeviceViewModel?.IsDimmableLightingControl ?? false;

    public int OnLevel
    {
        get => onLevel;
        set
        {
            if (onLevel == value) return;
            onLevel = value;
            OnPropertyChanged();
        }
    }
    private int onLevel;

    public double OnLevelAsFractionalPercent => OnLevel / 255.0;

    public int RampRate
    {
        get => rampRate;
        set
        {
            if (rampRate == value) return;
            rampRate = (byte)value;
            OnPropertyChanged();
        }
    }
    private int rampRate;

    public string RampRateAsString => ViewModel.Base.RampRate.ToString(RampRate);

    /// <summary>
    /// Name of the channel on the destination device that of this link
    /// if that decvice has multiple channels
    /// </summary>
    public string DeviceChannelName => (DeviceViewModel?.HasChannels ?? false) ? DeviceViewModel.GetChannelName(Group) : "";

    /// <summary>
    /// Replace this link by another one, usually an edited version of this link
    /// </summary>
    public void Replace(MemberViewModel newMember)
    {
        containingList.ReplaceMember(this, newMember);
    }

    /// <summary>
    /// Edit this link, opening UI as necessary
    /// </summary>
    public void Remove()
    {
        containingList.RemoveMember(this);
    }

    /// <summary>
    /// Pointer event handlers to create IsMouseOver
    /// Overriden here only to avoid intellisense error in XAML where these methods are referenced
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public override void PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.PointerEntered(sender, e);
    }

    public override void PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.PointerExited(sender, e);
    }

    // Show the On/Off button when the mouse is over the channel
    public override void PointerOverChanged()
    {
        OnPropertyChanged(nameof(IsEditButtonShown));
    }
}
