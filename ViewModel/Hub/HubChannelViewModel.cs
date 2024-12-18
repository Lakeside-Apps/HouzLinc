/* Cop7yright 2022 Christian Fortini

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

using ViewModel.Links;
using Insteon.Model;

namespace ViewModel.Hub;

/// <summary>
/// View model for a scene of the hub, presented in the detail panel or page of the "Hub Scenes" page
/// </summary>
public sealed class HubChannelViewModel : LinkHostViewModel, IChannelObserver
{
    private HubChannelViewModel(Device hub, Channel channel) : base(hub)
    {
        this.channel = channel;
    }

    /// <summary>
    /// Get or create a view model for the given HubChannel id
    /// Garantees that only one view model will be create for a given channel.
    /// </summary>
    /// <param name="hub"></param>
    /// <param name="channel"></param>
    /// <returns>View model</returns>
    public static HubChannelViewModel GetOrCreate(Device hub, Channel channel)
    {
        hubChannelViewModels ??= new Dictionary<int, HubChannelViewModel>();

        if (!hubChannelViewModels.TryGetValue(channel.Id, out HubChannelViewModel? hubChannelViewModel))
        {
            hubChannelViewModel = new HubChannelViewModel(hub, channel);
            hubChannelViewModels.Add(channel.Id, hubChannelViewModel);
        channel.AddObserver(hubChannelViewModel);
        }

        return hubChannelViewModel;
    }

    /// <summary>
    /// Get or create a view model for the given HubChannel id
    /// Garantees that only one view model will be create for a given channel.
    /// </summary>
    /// <param name="itemKey"></param>
    /// <returns>Found view model or null if no channel exists with this key</returns>
    public static HubChannelViewModel? GetOrCreateItemByKey(House house, string itemKey)
    {
        int channelId = int.Parse(itemKey);
        var hub = house.Hub;
        return hub != null ? HubChannelViewModel.GetOrCreate(hub, hub.Channels[channelId]) : null;
    }

    // Already created hub channel view models
    // TODO: multiple houses - the key in this dictionary needs to differenciate on the house or hub
    // if we ever want to support multiple houses concurrently
    private static Dictionary<int, HubChannelViewModel>? hubChannelViewModels;

    // Cache for the presence of links on a channel
    // TODO: multiple houses - the key in this set needs to differenciate on the house or hub
    // if we ever want to support multiple houses concurrently
    private static HashSet<int>? hubChannelHasLinks;

    /// <summary>
    /// Whether this channel has any links
    /// </summary>
    /// <returns></returns>
    public bool HasLinks
    {
        get
        {
            if (hubChannelHasLinks == null)
            {
                hubChannelHasLinks = new HashSet<int>();
                foreach (var record in Device.AllLinkDatabase)
                {
                    hubChannelHasLinks.Add(record.IsController ? record.Group : record.Data3);
                }
            }
            return hubChannelHasLinks.Contains(Id);
        }
    }

    // Call by base class when the AllLinkDatabase changes
    // Force rebuilding the Set of channels with links
    protected private override void RecordListChanged()
    {
        hubChannelHasLinks = null;
        OnPropertyChanged(nameof(HasLinks));
        OnPropertyChanged(nameof(IsOnOffButtonShown));
    }

    /// <summary>
    /// Notifies this HubCHannelViewModel that it is active (presented on screen) or inactive
    /// </summary>
    public override void ActiveStateChanged()
    {
        base.ActiveStateChanged();
        OnPropertyChanged(nameof(IsOnOffButtonShown));
        if (IsActive)
        {
            channel.House.Hub?.AllLinkDatabase.AddObserver(this);
        }
        else
        {
            channel.House.Hub?.AllLinkDatabase.RemoveObserver(this);
        }
    }

    // Channel this ViewModel is for
    private Channel channel;

    public string DisplayName => channel?.Name ?? string.Empty;
    public string DisplayNameAndId => channel != null ? channel.Name + " (" + channel.Id + ")" : string.Empty;
    public string DisplayId => channel?.Id.ToString() ?? string.Empty;

    public string? Name
    {
        get => channel != null ? channel.Name : string.Empty;
        set
        {
            if (value == "") value = null;
            if (value != channel.Name)
            {
                channel.Name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(DisplayNameAndId));
            }
        }
    }

    /// <summary>
    /// Indicate wherer the Scene Name can be edited in the UI at the moment
    /// </summary>
    public bool IsNameEditable
    {
        get
        {
            return _isNameEditable;
        }
        set
        {
            if (_isNameEditable != value)
            {
                _isNameEditable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNameReadOnly));
            }
        }
    }
    public bool IsNameReadOnly => !_isNameEditable;
    bool _isNameEditable;

    public int Id => this.channel != null ? this.channel.Id : 0;

    /// <summary>
    /// Unique key for this item
    /// </summary>
    public override string ItemKey => Id.ToString();

    // This class uses a grouped list with responders and controllers
    protected override bool UseGroupedLinkList { get; } = true;

    /// <summary>
    /// Filter the list of links to the current channel
    /// </summary>
    /// <param name="allLinkRecord"></param>
    /// <returns>Returns true to include</returns>
    protected override bool FilterLink(AllLinkRecord allLinkRecord)
    {
        if (allLinkRecord.IsController)
        {
            return allLinkRecord.Group == Id;
        }
        else
        {
            return allLinkRecord.Data3 == Id;
        }
    }

    /// <summary>
    /// Handler for the "Force Sync Hub" menu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ForceSyncDevice_Click(object sender, RoutedEventArgs e)
    {
        Device.ScheduleSync(forceRead: true);
    }

    /// <summary>
    /// Handler for the "Remove Stale Controllers/Responders" menu.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void RemoveStaleLinks_Click(Object sender, RoutedEventArgs e)
    {
        Device.RemoveStaleLinks();
    }

    /// <summary>
    /// Create a new responder link as input to the "Add New Responder Link" dialog
    /// Prepopulate Data3 (the responding channel) with channel currently in view
    /// </summary>
    /// <returns>LinkViewModel for the new link</returns>
    public override LinkViewModel CreateNewResponderLink()
    {
        LinkViewModel newLink = base.CreateNewResponderLink();
        newLink.Group = channel.Id;
        return newLink;
    }

    /// <summary>
    /// Create a new controller link as input to the "Add New Controller Link" dialog
    /// Prepopulate Group with channel currently in view
    /// </summary>
    /// <returns>LinkViewModel for the new link</returns>
    public override LinkViewModel CreateNewControllerLink()
    {
        LinkViewModel newLink = base.CreateNewControllerLink();
        newLink.Group = channel.Id;
        return newLink;
    }
    /// <summary>
    /// Turn on the responders on this channel
    /// For now only full level is supported
    /// </summary>
    public void SceneOn()
    {
        channel.ScheduleTurnOn(1.0f);
    }

    /// <summary>
    /// Turn on the responders on this channel
    /// </summary>
    public void SceneFullOn()
    {
        channel.ScheduleTurnOn(1.0f);
    }

    /// <summary>
    /// Turn off the responders on this channel
    /// </summary>
    public void SceneOff()
    {
        channel.ScheduleTurnOff();
    }

    /// <summary>
    /// Should the On/Off button be shown in the list of channels
    /// </summary>
    public bool IsOnOffButtonShown
    {
#if ANDROID || IOS
        get => HasLinks;
#else
        get => (IsActive || IsPointerOver) && HasLinks;
#endif
    }

    // Show the On/Off button when the mouse is over the channel
    public override void PointerOverChanged()
    {
        OnPropertyChanged(nameof(IsOnOffButtonShown));
    }

    void IChannelObserver.ChannelPropertyChanged(Channel channel, string? propertyName)
    {
        switch (propertyName)
        {
            case "Name":
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(DisplayNameAndId));
                break;
            default:
                OnPropertyChanged(propertyName);
                break;
        }
    }

    void IChannelObserver.ChannelSyncStatusChanged(Channel channel)
    {
    }
}
