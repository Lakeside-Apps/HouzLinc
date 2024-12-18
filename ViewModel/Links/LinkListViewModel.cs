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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ViewModel.Links;

[Bindable(true)]
public class LinkListViewModel : ObservableCollection<LinkViewModel>
{
    internal LinkListViewModel(LinkHostViewModel host)
    {
        this.host = host;
    }

    internal LinkListViewModel(LinkHostViewModel host, IEnumerable<LinkViewModel> links) : this(host)
    {
        foreach (var link in links)
        {
            Add(link);
        }
    }

    private readonly LinkHostViewModel host;
    public Device Device => this.host.Device;

    // Data binding support
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        // Let the base class handle it
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Whether the underlying AllLinkDataBase has changed and this list needs to be rebuilt
    /// </summary>
    internal bool Changed;

    /// <summary>
    /// Get a LinkViewModel in this list by its AllLinkRecord using the record uid
    /// This will get the correct LinkViewModel even if there are duplicates of the record
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    public LinkViewModel? GetLinkViewModel(AllLinkRecord record)
    {
        foreach (var linkViewModel in this)
        {
            if (linkViewModel.AllLinkRecord.Uid.Equals(record.Uid))
                return linkViewModel;   
        }
        return null;
    }

    /// <summary>
    /// Try to get the index of a LinkViewModel in this list by the record uid
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    public int GetLinkIndex(AllLinkRecord record)
    {
        for (var i = 0; i < Count; i++)
        {
            if (this[i].AllLinkRecord.Uid.Equals(record.Uid))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Currently selected link in the list
    /// </summary>
    public LinkViewModel? SelectedLink
    {
        get => _selectedLink;
        set
        {
            if (value != _selectedLink)
            {
                if (_selectedLink != null)
                    _selectedLink.IsSelected = false;
                _selectedLink = value;
                if (_selectedLink != null)
                    _selectedLink.IsSelected = true;
                OnPropertyChanged();
            }
        }
    }
    private LinkViewModel? _selectedLink;

    /// <summary>
    /// LinkViewModel currently being edited when creating or editing a link
    /// </summary>
    public LinkViewModel? EditedLink
    {
        get => _editedLinkViewModel;
        private set
        {
            _editedLinkViewModel = value;
            OnPropertyChanged();
        }
    }
    private LinkViewModel? _editedLinkViewModel;

    /// <summary>
    /// Add the link to the device database
    /// This will notify the view model to update the view
    /// And the background synchronization will write the database to the device
    /// </summary>
    public void AddNewLink(LinkViewModel newLink)
    {
        host.Device.AllLinkDatabase.AddRecord(newLink.AllLinkRecord);
    }

    /// <summary>
    /// Replace a given link by another one, usually an edited version of the same link
    /// </summary>
    internal void ReplaceLink(LinkViewModel existingLink, LinkViewModel newLink)
    {
        // Mark the link as non-synchronized, so that the user sees it as pending change
        existingLink.IsSynchronized = false;

        // This will notify the view model to update the view
        // And the background synchronization will write the database to the device
        host.Device.AllLinkDatabase.ReplaceRecord(existingLink.AllLinkRecord, newLink.AllLinkRecord);
    }

    /// <summary>
    /// Remove a given link
    /// </summary>
    public void RemoveLink(LinkViewModel link)
    {
        // Don't remove the link view model from the list yet, just mark is as removed and not synchronized
        // so that the user sees it as pending removal
        host.Device.AllLinkDatabase.RemoveRecord(new(link.AllLinkRecord) { SyncStatus = SyncStatus.Changed });
    }
}
