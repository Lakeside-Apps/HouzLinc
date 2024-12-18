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
using ViewModel.Devices;
using Common;
using System.Diagnostics.CodeAnalysis;
using ViewModel.Base;
using System.ComponentModel;

namespace ViewModel.Links;

/// <summary>
/// This class is a view model for a AllLinkRecord in the database of any device
/// It can represent either a controller or responder link
/// In the UI, links can show the following information:
///
/// Controllers (stored in the controlling device database):
/// - Name and id of the responding device
/// - Group (i.e., controlling channel on the controller device)
/// - List of complement responder links (with same group) on the responder device
/// - List of responding channels, i.e., channels these responder links represent
///
/// Responders (stored in the responding device database):
/// - Name and id of the controlling device
/// - Group (same as the complementing controller link on the controller device)
/// - Channel on the responding device this responder link represents
/// - On-Level for dimmable devices
/// - Ramp-rate for dimmable devices
///
/// </summary>
[Bindable(true)]
public class LinkViewModel : CollectionItemViewModel<LinkListViewModel, LinkViewModel>
{
    /// <summary>
    /// Create a LinkViewModel for a new link on given device
    /// </summary>
    /// <param name="containingList">LinkListViewModel containing this LinkViewModel</param>
    /// <param name="isController"></param>
    internal LinkViewModel(LinkListViewModel containingList, bool isController)
        : base(containingList)
    {
        destDeviceId = InsteonID.Null;
        IsController = isController;
    }

    /// <summary>
    /// Create a LinkViewModel for a exiting link on given device
    /// </summary>
    /// <param name="containingList">LinkListViewModel containing this LinkViewModel</param>
    /// <param name="allLinkRecord">link record in the device ALL-link database</param>
    internal LinkViewModel(LinkListViewModel containingList, AllLinkRecord allLinkRecord)
        : base(containingList)
    {
        Debug.Assert(allLinkRecord != null);
        Debug.Assert(device.AllLinkDatabase.Contains(allLinkRecord));
        this.allLinkRecord = allLinkRecord;
        destDeviceId = allLinkRecord.DestID;
        group = allLinkRecord.Group;
        IsController = allLinkRecord.IsController;
        isRemoved = !allLinkRecord.IsInUse;
        isSynchronized = allLinkRecord.SyncStatus == SyncStatus.Synced;
        SceneId = allLinkRecord.SceneId;
        data1 = allLinkRecord.Data1;
        data2 = allLinkRecord.Data2;
        data3 = allLinkRecord.Data3;
        SceneId = allLinkRecord.SceneId;
    }

    /// <summary>
    /// Create a new LinkViewModel as a copy of another one
    /// </summary>
    /// <param name="linkViewModel"></param>
    public LinkViewModel(LinkViewModel linkViewModel)
        : base(linkViewModel)
    {
        allLinkRecord = linkViewModel.allLinkRecord;
        destDeviceId = linkViewModel.DestDeviceId;
        group = linkViewModel.Group;
        IsController = linkViewModel.IsController;
        isRemoved = !linkViewModel.IsRemoved;
        isSynchronized = linkViewModel.IsSynchronized;
        data1 = linkViewModel.data1;
        data2 = linkViewModel.data2;
        data3 = linkViewModel.data3;
        SceneId = linkViewModel.SceneId;
    }

    /// <summary>
    /// Produces a LinkRecord from this LinkViewModel
    /// </summary>
    /// <returns></returns>
    internal AllLinkRecord AllLinkRecord => allLinkRecord?? new AllLinkRecord
    {
        DestID = DestDeviceId,
        Group = (byte)Group,
        IsController = IsController,
        IsInUse = !IsRemoved,
        Data1 = (byte)data1,
        Data2 = (byte)data2,
        Data3 = (byte)data3,
        SceneId = SceneId,
        SyncStatus = IsSynchronized ? SyncStatus.Synced : SyncStatus.Changed,
    };
    private AllLinkRecord? allLinkRecord = null;

    // source device of the link
    private Device device => containingList.Device;

    /// <summary>
    /// Private helper to retrieve the destination device for this link
    /// </summary>
    private Device? DestDevice => device.House.GetDeviceByID(DestDeviceId);

    /// <summary>
    /// Globally unique identifier for the record
    /// </summary>
    public uint Uid => AllLinkRecord.Uid;

    /// <summary>
    /// Complement link records:
    /// - A controller link can have one or more complement responder links in the destination device database
    ///   indicating the various channels (e.g., buttons of a keypad) responding to this controller link
    /// - A responder link should in theory have only one complement controller link in the destination device
    ///   database, yet duplicates sometimes exist
    /// </summary>
    public List<AllLinkRecord>? ComplementAllLinkRecords
    {
        get
        {
            if (device.TryFindLinkComplementLinks(AllLinkRecord, out List<AllLinkRecord>? complementLinkRecords))
            {
                return complementLinkRecords;
            }
            return null;
        }
    }

    /// <summary>
    /// Destination device ID
    /// This is the responder device for a controller link and the controller device for a responder link
    /// </summary>
    public InsteonID DestDeviceId
    {
        get => destDeviceId;
        set
        {
            destDeviceId = value;
            OnPropertyChanged(nameof(DestDeviceId));
            OnPropertyChanged(nameof(DestDeviceViewModel));
            OnPropertyChanged(nameof(DestDeviceModelName));
            OnPropertyChanged(nameof(DestDeviceHasRoom));
            OnPropertyChanged(nameof(DestDeviceRoom));
            OnPropertyChanged(nameof(DestDeviceHasChannels));
            OnPropertyChanged(nameof(DestDeviceChannelType));
            OnPropertyChanged(nameof(DestDeviceIsHub));
            OnPropertyChanged(nameof(ControllerDestDeviceHasChannels));
            OnPropertyChanged(nameof(ResponderDestDeviceHasChannels));
            OnPropertyChanged(nameof(ControllerDestDeviceChannelTypeIdAndName));
            OnPropertyChanged(nameof(ResponderDestDeviceChannelTypeIdsAndNames));
            OnPropertyChanged(nameof(DestDeviceDisplayNameAndId));
            OnPropertyChanged(nameof(ResponderDestDeviceFirstChannelId));
        }
    }
    private InsteonID destDeviceId;

    // Retrieve the destination device view model for this link
    // Returns null if the destination device does not exist
    private DeviceViewModel? DestDeviceViewModel => 
        DestDevice != null ? DeviceViewModel.GetOrCreate(DestDevice) : null;

    /// <summary>
    /// Retrieves the destination device name and id in a readable form
    /// </summary>
    public string DestDeviceDisplayNameAndId
    {
        get
        {
            if (DestDeviceViewModel != null)
            {
                return DestDeviceViewModel.DisplayNameAndId;
            }
            else
            {
                return "Device Not Found (" + DestDeviceId + ")";
            }
        }
    }

    /// <summary>
    /// Retrieves the model of the destination device of the link
    /// </summary>
    public string DestDeviceModelIconPath_72x72
    {
        get {
            if (DestDeviceViewModel != null)
            {
                if (DestDeviceViewModel.IsKeypadLinc)
                {
                    if (IsController)
                    {
                        return DestDeviceViewModel.GetModelIconPath_72x72(data3);
                    }
                    else
                    {
                        return DestDeviceViewModel.GetModelIconPath_72x72(Group);
                    }
                }
                else
                {
                    return DestDeviceViewModel.ModelIconPath_72x72;
                }
            }
            return "NotFound.png";
        }
    }

    /// <summary>
    /// Does the LinkViewModel have a destination device
    /// </summary>
    public bool HasDestDevice => DestDeviceViewModel != null;

    /// <summary>
    /// Retrieve the destination device model name
    /// </summary>
    public string DestDeviceModelName => DestDeviceViewModel?.ModelName ?? string.Empty;

    /// <summary>
    /// Retrieve the destination device room
    /// </summary>
    public string DestDeviceRoom => DestDeviceViewModel?.Room ?? string.Empty;

    /// <summary>
    /// Whether the destination device model has a room
    /// </summary>
    public bool DestDeviceHasRoom => (DestDeviceViewModel != null) ? DestDeviceViewModel.HasRoom : false;

    /// <summary>
    /// Whether this link is a controller link
    /// This is immutable, create a new LinkViewModel to change it
    /// </summary>
    public bool IsController { get; set; }

    /// <summary>
    /// Whether this link is a responder link
    /// This is immutable, create a new LinkViewModel to change it
    /// </summary>
    public bool IsResponder => !IsController;

    /// <summary>
    /// For a controller link on a device with channels, this is the controlling channel
    /// </summary>
    public int Group 
    { 
        get => group;
        set
        {
            if (group != value)
            {
                group = value;
                // Force recreation of underlying AllLinkRecord next time it is obtained
                allLinkRecord = null;
                OnPropertyChanged();
            }
        }
    }
    private int group;

    /// <summary>
    /// For a responder link on a device with channels, this is the responding channel
    /// on that device
    /// </summary>
    public int DeviceChannelId
    {
        get => IsResponder ? data3 : 0;
        set 
        { 
            if (IsResponder && data3 != value)
            {
                data3 = value;
                // Force recreation of underlying AllLinkRecord next time it is obtained
                allLinkRecord = null;
                OnPropertyChanged();
            }
        }
    }
    private int data3;

    /// <summary>
    /// Does this link have a complement link on the destination device
    /// </summary>
    public bool HasComplementLink => ComplementAllLinkRecords != null;

    /// <summary>
    /// Is this link synchronized, i.e., present on the physical device
    /// </summary>
    public bool IsSynchronized
    { 
        get => isSynchronized;
        set
        {
            if (isSynchronized != value)
            {
                isSynchronized = value;
                // Since SyncStatus can be changed on a link record, we check that the new value is actually
                // different from the underlying AllLinkRecord before forcing its re-creation
                if (allLinkRecord != null && 
                    allLinkRecord.SyncStatus != (isSynchronized ? SyncStatus.Synced : SyncStatus.Changed))
                {
                    allLinkRecord = null;
                }
                // Force recreation of underlying AllLinkRecord next time it is obtained
                OnPropertyChanged();
            }
        } 
    }
    private bool isSynchronized;

    /// <summary>
    /// Is this link removed, i.e., should be removed form the physical device
    /// </summary>
    public bool IsRemoved
    {
        get => isRemoved;
        set
        {
            if (isRemoved != value)
            {
                isRemoved = value;
                // Force recreation of underlying AllLinkRecord next time it is obtained
                allLinkRecord = null;
                OnPropertyChanged();
            }
        }
    }
    private bool isRemoved;

    /// <summary>
    /// On Level on links for dimmable devices
    /// If this is a responder link, the onlevel is data1
    /// If it is a controller link, the onlevel is data1 of the complementing 
    /// responder link in the destination device
    /// </summary>
    public int OnLevel
    {
        get => (IsResponder && device.IsDimmableLightingControl) ? data1 : 0;
        set
        {
            if (IsResponder && device.IsDimmableLightingControl)
            {
                data1 = value;
                // Force recreation of underlying AllLinkRecord next time it is obtained
                allLinkRecord = null;
                OnPropertyChanged();
            }
            // fail silently when trying to set on-level on a link that does not support it
        }
    }
    private int data1;

    /// <summary>
    /// Ramp rate on links for dimmable devices
    /// </summary>
    public int RampRate
    {
        get => (IsResponder && device.IsDimmableLightingControl) ? data2 : 0;
        set
        {
            if (IsResponder && device.IsDimmableLightingControl)
            {
                data2 = value;
                // Force recreation of underlying AllLinkRecord next time it is obtained
                allLinkRecord = null;
                OnPropertyChanged();
            }
            // Fail silently when trying to set ramp rate on a link that does not support it
        }
    }
    private int data2;

    /// <summary>
    /// For a responder link, returns the ramp rate as a string
    /// </summary>
    public string RampRateAsString => ViewModel.Base.RampRate.ToString(RampRate);

    /// <summary>
    /// Whether this link defines OnLevel and RampRate, i.e., is it a responder link on a dimmable device
    /// </summary>
    public bool HasRampRateAndOnLevel => IsResponder && device.IsDimmableLightingControl;

    /// <summary>
    /// Whether the destination device has channels
    /// </summary>
    [MemberNotNullWhen(true, nameof(DestDeviceViewModel))]
    public bool DestDeviceHasChannels => DestDeviceViewModel != null && DestDeviceViewModel.HasChannels;

    /// <summary>
    /// Whether the destination device is the hub
    /// </summary>
    [MemberNotNullWhen(true, nameof(DestDeviceViewModel))]
    public bool DestDeviceIsHub => DestDeviceViewModel != null && DestDeviceViewModel.IsHub;

    /// <summary>
    /// Whether this is a responder link and the (controller) destination device has channels
    /// </summary>
    [MemberNotNullWhen(true, nameof(DestDeviceViewModel))]
    public bool ControllerDestDeviceHasChannels => IsResponder && DestDeviceViewModel != null && DestDeviceViewModel.HasChannels;

    /// <summary>
    /// Whether this is a controller link and the (responder) destination device has channels
    /// </summary>
    [MemberNotNullWhen(true, nameof(DestDeviceViewModel))]
    public bool ResponderDestDeviceHasChannels => IsController && DestDeviceViewModel != null && DestDeviceViewModel.HasChannels;

    /// <summary>
    /// For any link, return a string describing the type of channels, if any, on the destination device
    /// e.g.: ", Scene:" or ", Button:"
    /// </summary>
    public string DestDeviceChannelType => DestDeviceHasChannels ? DestDeviceViewModel.ChannelType : string.Empty;

    /// <summary>
    /// For a responder link, return a string with the type of channels on the destination (controller) device,
    /// followed by the id of the controlling channel, followed by the name of the controlling channel
    /// e.g.: "Button 4: Main lights"
    /// </summary>
    public string ControllerDestDeviceChannelTypeIdAndName => IsResponder && DestDeviceHasChannels ? 
        ($"{ DestDeviceViewModel.ChannelType } { Group.ToString() } { (DestDeviceViewModel.GetChannelName(Group) != string.Empty ? "- " + DestDeviceViewModel.GetChannelName(Group) : string.Empty) }") : string.Empty;

    /// <summary>
    /// For a controller link, return the first responding channels ids and name on the (responder) destination device
    /// </summary>
    public int ResponderDestDeviceFirstChannelId => DestDeviceHasChannels && ComplementAllLinkRecords != null ? ComplementAllLinkRecords[0].Data3 : 0;

    /// <summary>
    /// For a controller link, return a string with the type of channels on the responder device
    /// followed by a comma delimited list of responding channels ids and name on the responder device
    /// </summary>
    public string ResponderDestDeviceChannelTypeIdsAndNames
    {
        get
        {
            if (IsController && DestDeviceHasChannels && ComplementAllLinkRecords != null)
            {
                string channelNames = string.Empty;
                foreach (AllLinkRecord record in ComplementAllLinkRecords)
                {
                    if (channelNames != string.Empty)
                        channelNames += ", ";
                    channelNames += $"{record.Data3.ToString()} - {DestDeviceViewModel.GetChannelName(record.Data3)}";
                }
                return $"{DestDeviceChannelType} {channelNames}";
            }
            else
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// The Id of the scene this link is part of
    /// </summary>
    public int SceneId { get; set; }

    /// <summary>
    /// Return true if this link is part of a scene
    /// </summary>
    public bool HasScene => SceneId != 0;

    /// <summary>
    /// Returns the display name of the scene this link is part of
    /// </summary>
    public string SceneName => device.House.Scenes.GetSceneById(SceneId)?.Name ?? string.Empty;

    /// <summary>
    /// Replace this link by another in the containing list
    /// </summary>
    public void Replace(LinkViewModel newLink)
    {
        containingList.ReplaceLink(this, newLink);
    }

    /// <summary>
    /// Remove this link in the containing list
    /// </summary>
    public void Remove()
    {
        containingList.RemoveLink(this);
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
