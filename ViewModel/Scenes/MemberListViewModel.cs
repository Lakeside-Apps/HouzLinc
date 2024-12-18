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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ViewModel.Base;
using ViewModel.Settings;

namespace ViewModel.Scenes;

public sealed class MemberListViewModel : SortableObservableCollection<MemberViewModel>, ISceneMembersObserver
{
    internal MemberListViewModel(SceneViewModel host)
    {
        this.host = host;
        host.Scene.Members.AddObserver(this);
    }

    private readonly SceneViewModel host;

    public SceneViewModel SceneViewModel => host;

    public MemberViewModel? FindMemberViewModel(SceneMember member)
    {
        return this.FirstOrDefault(m => m.Member.Equals(member));
    }

    // Data binding support
    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        // Let the base class handle it
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    internal void ReplaceItem(MemberViewModel item, MemberViewModel replacementItem)
    {
        int idx = IndexOf(item);
        SetItem(idx, replacementItem);
    }

    /// <summary>
    /// MemberViewModel currently selected in the view
    /// One time bindable
    /// </summary>
    public MemberViewModel? SelectedMember
    {
        get => selectedMember;
        set
        {
            if (value != selectedMember)
            {
                if (selectedMember != null)
                    selectedMember.IsSelected = false;
                selectedMember = value;
                if (selectedMember != null)
                    selectedMember.IsSelected = true;
                OnPropertyChanged();
            }
        }
    }
    private MemberViewModel? selectedMember;

    /// <summary>
    /// Create a new scene member initialized with the last used values saved to the settings store
    /// if they correspond to a device that already exists
    /// </summary>
    public MemberViewModel CreateNewSceneMemberWithLastUsedValue()
    {
        var deviceIdString = SettingsStore.ReadLastUsedValue("SceneMemberDeviceId") as string;
        var deviceId = deviceIdString != null ? new InsteonID(deviceIdString) : InsteonID.Null;
 
        if (host.Scene.House.GetDeviceByID(deviceId) != null)
        {
            return new MemberViewModel(this, new SceneMember(host.Scene) { DeviceId = deviceId })
            {
                Group = SettingsStore.ReadLastUsedValueAsInt("SceneMemberChannel", 0),
                IsController = SettingsStore.ReadLastUsedValueAsInt("SceneMemberIsController", 0) != 0,
                IsResponder = SettingsStore.ReadLastUsedValueAsInt("SceneMemberIsResponder", 0) != 0,
                OnLevel = SettingsStore.ReadLastUsedValueAsInt("SceneMemberOnLevel", 255),
                RampRate = SettingsStore.ReadLastUsedValueAsInt("SceneMemberRampRate", 0)
            };
        }
        else
        {
            return new MemberViewModel(this, new SceneMember(host.Scene));
        }
    }

    /// <summary>
    /// Save last used value for the give member
    /// </summary>
    /// <param name="member"></param>
    public void StoreSceneMemberLastUsedValues(MemberViewModel member)
    {
        SettingsStore.WriteLastUsedValue("SceneMemberDeviceId", member.DeviceId.ToString());
        SettingsStore.WriteLastUsedValue("SceneMemberChannel", member.Group);
        SettingsStore.WriteLastUsedValue("SceneMemberIsController", member.IsController ? 1 : 0);
        SettingsStore.WriteLastUsedValue("SceneMemberIsResponder", member.IsResponder ? 1 : 0);
        SettingsStore.WriteLastUsedValue("SceneMemberOnLevel", member.OnLevel);
        SettingsStore.WriteLastUsedValue("SceneMemberRampRate", member.RampRate);
    }

    /// <summary>
    /// Add new scene member
    /// Update to this view model will happen when the model notifies us
    /// </summary>
    /// <param name="newMember"></param>
    public void AddNewMember(MemberViewModel newMember)
    {
        host.Scene.AddMember(newMember.Member);
    }

    /// <summary>
    /// Remove a member from this scene
    /// </summary>
    /// <param name="removedMember"></param>
    internal void RemoveMember(MemberViewModel removedMember)
    {
        // Remove this member in the model
        // Update to this view model will happen when the model notifies us
        host.Scene.RemoveMember(removedMember.Member, removeLinks: true);
    }

    /// <summary>
    /// Replace a member from this scene by another one
    /// </summary>
    /// <param name="existingMember">member to replace</param>
    /// <param name="newMember">member to replace by</param>
    /// <returns></returns>
    internal void ReplaceMember(MemberViewModel existingMember, MemberViewModel newMember)
    {
        // Replace this member in the model
        // Update to this view model will happen when the model notifies us
        host.Scene.ReplaceMember(existingMember.Member, newMember.Member);
    }

    /// <summary>
    /// Sort the list by different keys
    /// </summary>
    /// <param name="direction"></param>
    public void SortByMember(SortDirection direction)
    {
        base.Sort<MemberViewModel>(x => x, direction, new MemberViewModelComparer());
    }

    public void SortByDisplayName(SortDirection direction)
    {
        base.Sort<string>(x => x.DeviceDisplayName, direction);
    }

    // Observer methods
    // Handle changes to the scene members and member list

    void ISceneMembersObserver.SceneMembersCleared(Scene scene)
    {
        ClearItems();
    }

    void ISceneMembersObserver.SceneMemberAdded(Scene scene, SceneMember member)
    {
        Add(new MemberViewModel(this, member));
    }

    void ISceneMembersObserver.SceneMemberReplaced(Scene scene, SceneMember memberToReplace, SceneMember newMember)
    {
        var replacedMember = FindMemberViewModel(memberToReplace);
        if (replacedMember != null)
        {
            var indexInThis = IndexOf(replacedMember);
            this[indexInThis] = new MemberViewModel(this, newMember);
        }
    }

    void ISceneMembersObserver.SceneMemberRemoved(Scene scene, SceneMember member)
    {
        var removedMember = FindMemberViewModel(member);
        if (removedMember != null)
        {
            Remove(removedMember);
        }
    }
}
