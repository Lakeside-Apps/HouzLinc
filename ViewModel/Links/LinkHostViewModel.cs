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

using Insteon.Model;
using ViewModel.Base;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Data;

namespace ViewModel.Links;

/// <summary>
/// Collection of groups when the links are grouped by type (controller or responder)
/// </summary>
[System.ComponentModel.Bindable(true)]
public sealed class LinkGroup : LinkListViewModel
{
    public LinkGroup(LinkHostViewModel linkHost, IEnumerable<LinkViewModel> links, bool isController) : base(linkHost, links)
    {
        this.LinkHost = linkHost;
        this.IsController = isController;
    }

    public LinkGroup(LinkHostViewModel linkHost, bool isController) :
        this(linkHost, new LinkListViewModel(linkHost), isController)
    {
    }

    public LinkHostViewModel LinkHost { get; private set; }
    public bool IsController { get; private set; }
    public string GroupHeader => IsController ? "Responders" : "Controllers";
    public string GroupType => IsController ? "Responder" : "Controller";
}

/// <summary>
/// Link template selector for lists containing both controllers and responders links
/// </summary>
public sealed class LinkTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ControllerLinkTemplate { get; set; }
    public DataTemplate? ResponderLinkTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is LinkViewModel link)
        {
            return link.IsController ? ControllerLinkTemplate : ResponderLinkTemplate;
        }
        return null;
    }
}

/// <summary>
/// A base class to build and hold controller and responder links on a device or the hub
/// </summary>
public abstract class LinkHostViewModel : ItemViewModel, IAllLinkDatabaseObserver
{
    internal LinkHostViewModel(Device d)
    {
        Device = d;

        // Create empty link lists to avoid having to check for null
        controllerLinks = new LinkListViewModel(this);
        responderLinks = new LinkListViewModel(this);
        links = new CollectionViewSource();
    }

    /// <summary>
    /// Underlying device in the model
    /// </summary>
    // Underlying model device
    public readonly Device Device;

    /// <summary>
    /// Information about channels on this device, implemented by derived classes
    /// but accessed by UI using LinkHostViewModel as data context
    /// </summary>
    public virtual string ChannelType => string.Empty;
    public virtual bool HasChannels => false;
    public virtual int CurrentChannelId => 0;
    public virtual string CurrentChannelName => string.Empty;
    // To quote the channel name in the binding string ('' or \' or &apos; don't work)
    public string QuotedCurrentChannelName => $"'{CurrentChannelName}'";

    // Whether the derived class uses a single grouped list (HubChannel)
    // or two separate lists for controllers and responders (other devices)
    protected virtual bool UseGroupedLinkList { get; } = false;

    /// <summary>
    /// List of responder links (LinkListViewModel) on this device,
    /// For the current group/button for certain devices
    /// Bindable multiple times
    /// </summary>
    public LinkListViewModel ResponderLinks
    {
        get => responderLinks;
        private set
        {
            responderLinks = value;
            OnPropertyChanged();
        }
    }
    private LinkListViewModel responderLinks;

    /// <summary>
    /// List of controller links (LinkListViewModel) on this device, 
    /// For the current group/button for certain devices
    /// Bindable multiple times
    /// </summary>
    public LinkListViewModel ControllerLinks
    {
        get => controllerLinks;
        private set
        {
            controllerLinks = value;
            OnPropertyChanged();
        }
    }
    private LinkListViewModel controllerLinks;

    /// <summary>
    /// View source from a list of groups of links, grouped by link type (controller or responder)
    /// For the current group/button for certain devices
    /// Bindable multiple times
    /// </summary>
    public CollectionViewSource Links
    {
        get => links;
        set 
        { 
            links = value;
            OnPropertyChanged();
        }
    }
    private CollectionViewSource links;

    // IAllLinkDatabaseObserver implementation to listen to changes in the database
    void IAllLinkDatabaseObserver.AllLinkDatabaseSyncStatusChanged(Device? device)
    {
        // This does not affect the UI at this time
    }

    void IAllLinkDatabaseObserver.AllLinkDatabasePropertiesChanged(Device? device)
    {
        // This does not affect the UI
        // Current properties are: nextRecordToRead, Revision, LastUpdate
    }

    void IAllLinkDatabaseObserver.AllLinkDatabaseCleared(Insteon.Model.Device? device)
    {
        // Clear the lists
        controllerLinks.Clear();
        responderLinks.Clear();
        ScheduleRebuildLinksIfActive();
        RecordListChanged();
    }

    void IAllLinkDatabaseObserver.AllLinkRecordAdded(Device? device, AllLinkRecord record)
    {
        if (device == null) return;
        var linkListViewModel = GetLinkListViewModel(record);
        // TODO: if we use a single grouped list, the new link should be inserted
        // at the end of the controller group or the responder group depending on its type.
        linkListViewModel.Add(new LinkViewModel(linkListViewModel, record));
        RecordListChanged();
    }

    void IAllLinkDatabaseObserver.AllLinkRecordRemoved(Device? device, AllLinkRecord record)
    {
        if (device == null) return;
        RemoveRecordFromList(record);
        RecordListChanged();
    }

    void RemoveRecordFromList(AllLinkRecord record)
    {
        var linkListViewModel = GetLinkListViewModel(record);
        var index = linkListViewModel.GetLinkViewModelIndex(record);
        if (index >= 0)
            linkListViewModel.RemoveAt(index);
        RecordListChanged();
    }

    void IAllLinkDatabaseObserver.AllLinkRecordReplaced(Device? device, AllLinkRecord recordToReplace, AllLinkRecord newRecord)
    {
        if (device == null) return;

        if (newRecord.SyncStatus == SyncStatus.Synced && !newRecord.IsInUse)
        {
            // We don't show deleted & synced records in the list of link view models
            // (note that we exlude these in BuildLinkList as well)
            RemoveRecordFromList(recordToReplace);
        }
        else
        {
            var linkListViewModel = GetLinkListViewModel(newRecord);
            var index = linkListViewModel.GetLinkViewModelIndex(recordToReplace);
            if (index >= 0)
            {
                linkListViewModel[index] = new LinkViewModel(linkListViewModel, newRecord);
            }
            else
            {
                linkListViewModel.Add(new LinkViewModel(linkListViewModel, newRecord));
            }
        }
        RecordListChanged();
    }

    // Called when associated AllLinkDatabase changes
    // Implemented by derived classes
    protected private virtual void RecordListChanged()
    {
    }

    // Helper to get the correct LinkListViewModel for a given AllLinkRecord
    LinkListViewModel GetLinkListViewModel(AllLinkRecord record)
    {
        if (UseGroupedLinkList)
            // Using a grouped list, return the LinkListViewModel for the group that matches the record type
            return (Links.Source as ObservableCollection<LinkGroup>)!
                .FirstOrDefault(g => g.IsController == record.IsController) ?? new LinkListViewModel(this);
        else if (record.IsController)
            return controllerLinks;
        else
            return responderLinks;
    }

    /// <summary>
    /// If active, schedule rebuilding LinkViewModel(s) as needed.
    /// If not active but force is true, we'll rebuild next time we are activated (see ActiveStateChange).
    /// This can optionally delay jobs by 250ms and cancel any pending job before scheduling a new run.
    /// This helps keep performance up when the user is rapidely switching between devices or channels.
    /// </summary>
    /// <param name="cancelPendingJob">See above</param>
    internal void ScheduleRebuildLinksIfActive(bool cancelPendingJob = false)
    {
        if (IsActive)
        {
            if (!UseGroupedLinkList)
            {
                ScheduleBuildLinkLists(cancelPendingJob);
            }
            else
            {
                ScheduleBuildGroupedLinkList(cancelPendingJob);
            }
        }
    }

    // Helper to generate the list of LinkViewModels in this device database.
    // Links are build asynchronously and the results are stored in ControllerLinks and ResponderLinks.
    // Jobs can be delayed by 250ms and cancelled when scheduling a new job.
    // This reduces uncessary work when switching between device rapidely.
    private void ScheduleBuildLinkLists(bool cancelPendingJob)
    {
        // If asked to cancel a pending job and we have a previous job pending
        // for the same type of links, attempt to cancel it
        if (cancelPendingJob)
        {
            if (linkJob != null)
            {
                UIScheduler.Instance.CancelJob(linkJob);
                linkJob = null;
            }
        }

        // Don't rebuild the links if a rebuild is already pending
        if (!UIScheduler.Instance.IsPending(linkJob))
        {
            linkJob = UIScheduler.Instance.AddAsyncJob("Rebuilding Controller Link Lists",
                async () =>
                {
                    // Capture and restore selection to prevent GridView from clearing it when ItemsSource changes
                    var selectedLink = _selectedLink;

                    ControllerLinks = await Task.Run(() => { return BuildLinkList(forControllers: true); });
                    ResponderLinks = await Task.Run(() => { return BuildLinkList(forControllers: false); });
                    linkJob = null;

                    // Propagate the selected link to the new list if there was one
                    if (selectedLink != null)
                    {
                        if (selectedLink.IsController)
                            SelectedControllerLink = ControllerLinks.GetLinkViewModel(selectedLink.AllLinkRecord);
                        else
                            SelectedResponderLink = ResponderLinks.GetLinkViewModel(selectedLink.AllLinkRecord);
                    }

                    return true;
                },
                completionCallback: null, group: null,
                delay: cancelPendingJob ? TimeSpan.FromMilliseconds(250) : TimeSpan.Zero);
        }
    }

    // Helper to generate the list of all LinkViewModel's (controllers or responders) in this device database.
    // This returns a CollectionViewSource that can be used to group the links by type (controller or responder)
    // This is currently only used by the HubHostViewModel.
    // Links are build asynchronously and the results are stored in Links
    private void ScheduleBuildGroupedLinkList(bool cancelPendingJob)
    {
        if (cancelPendingJob)
        {
            if (linkJob != null)
            {
                UIScheduler.Instance.CancelJob(linkJob);
                linkJob = null;
            }
        }

        // Don't rebuild the links if a rebuild is already pending
        if (!UIScheduler.Instance.IsPending(linkJob))
        {
            linkJob = UIScheduler.Instance.AddAsyncJob("Rebuilding Link Lists",
                async () =>
                {
                    // Capture and restore selection to prevent GridView from clearing it when ItemsSource changes
                    var selectedLink = _selectedLink;

                    var groupedLinks = await Task.Run(() =>
                    {
                        var query = from link in BuildLinkList()
                                    group link by link.IsController into g
                                    orderby g.Key ascending
                                    select new LinkGroup(this, links: g, isController: g.Key);

                        var gl = new ObservableCollection<LinkGroup>(query);
                        // Ensure that both group headers are shown
                        if (gl.Count == 0)
                        {
                            gl.Add(new LinkGroup(this, isController: false));
                            gl.Add(new LinkGroup(this, isController: true));
                        }
                        else if (gl.Count == 1)
                        {
                            if (gl[0].IsController)
                                gl.Insert(0, new LinkGroup(this, isController: false));
                            else
                                gl.Add(new LinkGroup(this, isController: true));
                        }
                        return gl;
                    });

                    Links = new CollectionViewSource { Source = groupedLinks, IsSourceGrouped = true };
                    linkJob = null;

                    // Propagate the selected link to the new list if there was one
                    if (selectedLink != null)
                    {
                        var linkListViewModel = GetLinkListViewModel(selectedLink.AllLinkRecord);
                        SelectedLink = linkListViewModel.GetLinkViewModel(selectedLink.AllLinkRecord);
                    }
                    return true;
                },
                completionCallback: null, group: null,
                delay: cancelPendingJob ? TimeSpan.FromMilliseconds(250) : TimeSpan.Zero);
        }
    }

    private static object? linkJob;

    // Helper to build the list of LinkViewModels
    // Depending on forController, this can build the list of controllers, responders,
    // or all links matching the filter criteria.
    internal LinkListViewModel BuildLinkList(bool? forControllers = null)
    {
        var linkListViewModel = new LinkListViewModel(this);
        foreach (AllLinkRecord linkRecord in Device.AllLinkDatabase)
        {
            // We don't include deleted links that have been synced with the physical device
            // since they don't actually exist. We do include deleted links that have not been
            // synced yet and let the UI show them as pending sync.
            if ((forControllers == null || linkRecord.IsController == forControllers) && 
                (linkRecord.IsInUse || linkRecord.SyncStatus != SyncStatus.Synced))
            {
                if (FilterLink(linkRecord))
                {
                    LinkViewModel linkViewModel = new LinkViewModel(linkListViewModel, linkRecord);
                    linkListViewModel.Add(linkViewModel);
                }
            }
        }
        return linkListViewModel;
    }

    /// <summary>
    /// This tracks the selected link in the grouped list
    /// </summary>
    public LinkViewModel? SelectedLink
    {
        get => _selectedLink;
        set
        {
            if (_selectedLink != value)
            {
                SwapSelectedLink(value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// This tracks the last selected link in either the responder
    /// or the controller link list
    /// </summary>
    public LinkViewModel? SelectedResponderLink
    {
        get => _selectedLink;
        set
        {
            if ((!isControllerLinkSelected || value != null) && _selectedLink != value)
            {
                isControllerLinkSelected = false;
                SwapSelectedLink(value);
                OnPropertyChanged();
            }
        }
    }

    public LinkViewModel? SelectedControllerLink
    {
        get => _selectedLink;
        set
        {
            if ((isControllerLinkSelected || value != null) && _selectedLink != value)
            {
                isControllerLinkSelected = true;
                SwapSelectedLink(value);
                OnPropertyChanged();
            }
        }
    }

    // Helper to swap the selected link
    private void SwapSelectedLink(LinkViewModel? value)
    {
        if (_selectedLink != null)
            _selectedLink.IsSelected = false;
        _selectedLink = value;
        if (_selectedLink != null)
            _selectedLink.IsSelected = true;
    }

    private LinkViewModel? _selectedLink;
    bool isControllerLinkSelected = false;

    /// <summary>
    /// Notifies this LinkHostViewModel that it is active (presented on screen) or inactive
    /// Also called when the connected state changes (IsConnected)
    /// </summary>
    public override void ActiveStateChanged()
    {
        base.ActiveStateChanged();

        if (IsActive)
        {
            ScheduleRebuildLinksIfActive(cancelPendingJob: true);
            Device.AllLinkDatabase.AddObserver(this);
        }
        else
        {
            Device.AllLinkDatabase.RemoveObserver(this);
        }
    }

    /// <summary>
    /// Custom filtering handler for derived classes to subset the lists of links
    /// Returns true if the link should be included in the lists
    /// </summary>
    /// <param name="linkRecord"></param>
    /// <returns>Returns true to include</returns>
    protected virtual bool FilterLink(AllLinkRecord linkRecord) { return true; }

    /// <summary>
    /// Create a new controller link as input to the "Add New Responder Link" dialog
    /// Device classes can override to preset certain properties on the new link
    /// </summary>
    /// <returns></returns>
    public virtual LinkViewModel CreateNewControllerLink()
    {
        return new LinkViewModel(controllerLinks, isController: true);
    }

    /// <summary>
    /// Create a new responder link as input to the "Add New Responder Link" dialog
    /// Device classes can override to preset certain properties on the new link
    /// </summary>
    /// <returns></returns>
    public virtual LinkViewModel CreateNewResponderLink()
    {
        return new LinkViewModel(responderLinks, isController: false);
    }
}
