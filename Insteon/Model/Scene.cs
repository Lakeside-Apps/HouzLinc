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
using Common;
using static Insteon.Model.AllLinkRecord;

namespace Insteon.Model;

// This class represents a scene not implemented by the hub, 
// but instead by creating links between each controller and responder
public sealed class Scene
{
    public Scene(Scenes scenes, string name, int id)
    {
        this.name = name;
        this.Id = id;
        this.Scenes = scenes; 
    }

    /// <summary>
    /// Shallow copy constructor.
    /// This does not fire property change notifications.
    /// Members is just a reference copy, not a deep copy.
    /// </summary>
    /// <param name="scene">Scene to copy from</param>
    /// <param name="id">New scene id</param>
    public Scene(Scene scene, int id)
    {
        Scenes = scene.Scenes;
        Id = id;
        name = scene.Name;
        lastTrigger = scene.LastTrigger;
        notes = scene.Notes;
        members = scene.Members;
    }

    public override bool Equals(object? other)
    {
        if (other != null && other is Scene otherScene)
        {
            Debug.Assert(!ReferenceEquals(this, other) || Id == otherScene.Id);
            return Id == otherScene.Id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Id;
    }

    // Copy state from another Scene.
    // This sends property change notifications to observers.
    internal void CopyFrom(Scene fromScene)
    {
        Name = fromScene.Name;
        LastTrigger = fromScene.LastTrigger;
        Notes = fromScene.Notes;
        Members = Members.CopyFrom(fromScene.Members);
    }

    // Whether this scene is identical to another scene.
    // Used primarily for test and DEBUG checks.
    internal bool IsIdenticalTo(Scene scene)
    {
        if (Name != scene.Name ||
            LastTrigger != scene.LastTrigger ||
            Notes != scene.Notes)
        {
            return false;
        }
        if (!Members.IsIdenticalTo(scene.Members))
        {
            return false;
        }
        return true;
    }

    internal void OnDeserialized()
    {
        isDeserialized = true;
        Members.OnDeserialized();
        AddObserver(House.ModelObserver);
    }
    private bool isDeserialized;

    /// <summary>
    /// For observers to subscribe to change notifications
    /// </summary>
    /// <param name="observer"></param>
    public Scene AddObserver(ISceneObserver observer)
    {
        observers.Add(observer);
        return this;
    }

    /// <summary>
    /// For observers to unsubscribe to change notifications
    /// </summary>
    /// <param name="observer"></param>
    public Scene RemoveObserver(ISceneObserver observer)
    {
        observers.Remove(observer);
        return this;
    }

    private List<ISceneObserver> observers = new();

    /// <summary>
    /// Containing scene list
    /// </summary>
    public Scenes Scenes { get; init; }

    /// <summary>
    /// House this scene belongs to
    /// </summary>
    public House House => Scenes.House;

    /// <summary>
    /// Read-only scene id
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Read-write scene properties
    /// </summary>
    public string Name
    { 
        get => name ?? string.Empty;
        set
        {
            if (name != value)
            {
                name = value;
                observers.ForEach (o => o.ScenePropertyChanged(this));
            }
        }
    }
    private string? name;

    public string? Room
    {
        get => room;
        set
        {
            if (room != value)
            {
                room = value;
                if (isDeserialized)
                {
                    House.Rooms.OnRoomsChanged();
                }
                observers.ForEach(o => o.ScenePropertyChanged(this));
            }
        }
    }
    private string? room;

    public string? LastTrigger 
    { 
        get => lastTrigger;
        set
        {
            if (lastTrigger != value)
            {
                lastTrigger = value;
                observers.ForEach (o => o.ScenePropertyChanged(this));
            }
        }
    }
    private string? lastTrigger;

    public string? Notes 
    { 
        get => notes;
        set
        {
            if (notes != value)
            {
                notes = value;
                observers.ForEach (o => o.ScenePropertyChanged(this));
            }
        }
    }
    private string? notes;

    public SceneMembers Members
    {
        get => members ??= new SceneMembers(this).AddObserver(House.ModelObserver);
        set
        {
            if (members != value)
            {
                members = value;
                observers.ForEach(o => o.SceneMembersChanged(this, members));
            }
        }
    }
    private SceneMembers? members;

    /// <summary>
    /// Expand this scene by making sure all links it generates are present on appropriate devices
    /// Note: this will not remove links after deleting a scene member, use RemoveSceneMember for that instead
    /// </summary>
    public void Expand()
    {
        foreach (SceneMember controllerMember in this.Members)
        {
            if (controllerMember.IsController)
            {
                foreach (SceneMember responderMember in this.Members)
                {
                    if (responderMember.IsResponder)
                    {
                        EnsureControllerLinkRecord(controllerMember, responderMember);
                        EnsureResponderLinkRecord(controllerMember, responderMember);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Add a member to a scene
    /// </summary>
    /// <param name="member"></param>
    public void AddMember(SceneMember member)
    {
        // If the member to add alaready exists in the scene, we're done
        // as we don't want to duplicate any member
        if (Members.TryGetMember(member, out SceneMember? memberToAdd))
            return;

        Members.Add(member);
        Expand();
    }

    /// <summary>
    /// Remove a scene member from a scene and remove corresponding links
    /// </summary>
    /// <param name="member">Member to remove (looked up by value)</param>
    public bool RemoveMember(SceneMember member, bool removeLinks)
    {
        if (!Members.TryGetMember(member, out SceneMember? memberToRemove))
        {
            Debug.Assert(false, $"Attempting to remove member matching {member.DeviceId}, group: {member.Group}, {(member.IsController ? "controller, " : "")}{(member.IsResponder ? "responder, " : "")}none found in this scene");
            return false;
        }

        if (removeLinks)
        {
            // Remove appropriate links to reflect member removal
            RemoveLinksForMemberRemoval(memberToRemove);
        }

        // Remove member from the scene
        Members.Remove(memberToRemove);

        return true;
    }

    /// <summary>
    /// Remove all members from a scene and remove corresponding links
    /// </summary>
    public void RemoveAllMembers()
    {
        foreach(var member in Members)
        {
            // Remove appropriate links to reflect member removal
            RemoveLinksForMemberRemoval(member);
        }
        Members.Clear();
    }

    /// <summary>
    /// Replace a scene member of given deviceId and group by another scene member
    /// </summary>
    /// <param name="oldMember">Member to replace, looked up by value</param>
    /// <param name="newMember">New member to replace with</param>
    /// <returns>false if member with this deviceId and group did not exist in the scene</returns>
    public bool ReplaceMember(SceneMember oldMember, SceneMember newMember)
    {
        // Nothing to do if old (replaced) and new members are the same
        if (newMember.Equals(oldMember))
            return true;

        if (!Members.TryGetMember(oldMember, out SceneMember? replacedMember))
        {
            // The device/group to replace is not in this scene, fail
            Debug.Assert(false, $"Attempting to replace scene member {oldMember.DeviceId}, Group: {oldMember.Group}, {(oldMember.IsController ? "controller, " : "")}{(oldMember.IsResponder ? "responder, " : "")} which does not exist in this scene");
            return false;
        }

        if (!newMember.IsController && !newMember.IsResponder)
        {
            // New member is neither controller nor responder, handle this as removal
            RemoveMember(replacedMember!, removeLinks: true);
            return true;
        }

        // See if we need to remove the links associated to the replaced member
        if (replacedMember.Group != newMember.Group)
        {
            // New member has different group, do a full replacement
            RemoveLinksForMemberRemoval(replacedMember);
        }
        else if (replacedMember.IsController && !newMember.IsController)
        {
            // Loosing the controller, remove only links associated to the controller
            RemoveLinksForMemberRemoval(replacedMember, keepResponder: true);
        }
        else if (replacedMember.IsResponder && !newMember.IsResponder)
        {
            // Loosing the responder, remove only links associated to the responder
            RemoveLinksForMemberRemoval(replacedMember, keepController: true);
        }
        else
        {
            // We are just editing the data in the existing scene member
            newMember = new SceneMember(replacedMember)
            {
                Data1 = newMember.Data1,
                Data2 = newMember.Data2,
                Data3 = newMember.Data3
            };
        }

        int idx = Members.IndexOf(replacedMember);
        Members[idx] = newMember;
        Expand();
        return true;
    }

    /// <summary>
    /// Remove stale members from a scene, i.e., members for inexistant devices
    /// </summary>
    public void RemoveStaleMembers()
    {
        List<SceneMember> staleMembers = new List<SceneMember>();

        foreach (var member in Members)
        {
            if (!House.Devices.TryGetEntry(member.GetHashCode(), out _))
            {
                staleMembers.Add(member);
            }
        }

        foreach (var member in staleMembers)
        {
            RemoveMember(member, removeLinks: true);
        }
    }

    /// <summary>
    /// Remove duplicate members from a scene
    /// </summary>
    public void RemoveDuplicateMembers()
    {
        Members.RemoveDuplicateEntries((SceneMember member) =>
        {
            Members.Remove(member);
            RemoveLinksForMemberRemoval(member, removeOnlyDuplicates: true);
        });
    }

    // Helper to remove links when a member is about to be removed
    private void RemoveLinksForMemberRemoval(SceneMember member, bool keepController = false, bool keepResponder = false, bool removeOnlyDuplicates = false)
    {
        if (!keepController && member.IsController)
        {
            foreach (SceneMember responderMember in this.Members)
            {
                if (responderMember.IsResponder)
                {
                    DeleteControllerLinkRecord(member, responderMember, removeOnlyDuplicates);
                    DeleteResponderLinkRecord(member, responderMember, removeOnlyDuplicates);
                }
            }
        }

        if (!keepResponder && member.IsResponder)
        {
            // Determine whether we have more responders on the same device than the responder being deleted.
            // If yes, we will not delete the controller links as they are still needed for this/these other responder(s)
            bool deleteControllerLink = false;
            if (Members.TryGetMatchingResponders(member.DeviceId, out List<SceneMember>? respondersOnSameDevice))
            {
                Debug.Assert(respondersOnSameDevice != null && respondersOnSameDevice.Count > 0);
                deleteControllerLink = respondersOnSameDevice.Count == 1;
            }

            foreach (SceneMember controllerMember in this.Members)
            {
                if (controllerMember.IsController)
                {
                    if (deleteControllerLink)
                    {
                        DeleteControllerLinkRecord(controllerMember, member, removeOnlyDuplicates);
                    }
                    DeleteResponderLinkRecord(controllerMember, member, removeOnlyDuplicates);
                }
            }
        }
    }

    // Ensure that the controller link from a scene controller to a responder is present and correct
    private void EnsureControllerLinkRecord(SceneMember controllerMember, SceneMember responderMember)
    {
        var controller = House.Devices.GetDeviceByID(controllerMember.DeviceId);
        if (controller == null)
        {
            // If the controller device does not exist, there is no link to create
            return;
        }

        if (responderMember.DeviceId == controllerMember.DeviceId)
        {
            if (responderMember.Group == controllerMember.Group)
            {
                // Controller and responder are the same, no need for a link
                return;
            }

            // Controller and responder are the same device but different groups, no controller link is needed
            // (we will set the follow behavior on the responder keypad button in EnsureResponderLinkRecord)
            if (controller/*== responder*/.IsKeypadLinc)
            {
                return;
            }
        }

        var responder = House.Devices.GetDeviceByID(responderMember.DeviceId);
        if (responder == null)
        {
            // If the responder device does not exist, there is no link to create
            return;
        }

        AllLinkRecord? existingRecord = null;

        // Create controller link record
        AllLinkRecord controllerRecord = new AllLinkRecord
        {
            DestID = responderMember.DeviceId,
            IsController = true,
            Group = (byte)controllerMember.Group,
            SceneId = this.Id,
            Data1 = 0x03,                         // 3 retries
            Data2 = controllerMember.Data2,       // listed as unused but seems to be set to ramp rate
            Data3 = (byte)controllerMember.Group, // controller device group/button
            SyncStatus = SyncStatus.Changed,
        };

        // Look for a match
        if (controller.AllLinkDatabase.TryGetMatchingEntries(controllerRecord, IdGroupTypeComparer.Instance, out List<AllLinkRecord>? matchingRecords))
        {
            foreach (AllLinkRecord record in matchingRecords)
            {
                if (record.SceneId == 0 && existingRecord == null)
                {
                    existingRecord = record;
                    break;
                }
                else if (record.SceneId == this.Id)
                {
                    existingRecord = record;
                    break;
                }
            }
        }

        if (existingRecord != null)
        {
            if (!existingRecord.Equals(controllerRecord))
            {
                // Update the existing record
                AllLinkRecord updatedRecord = new(existingRecord)
                {
                    Data1 = controllerRecord.Data1,
                    Data2 = controllerRecord.Data2,
                    Data3 = controllerRecord.Data3,
                    SceneId = Id,
                    SyncStatus = SyncStatus.Changed,
                };
                Debug.Assert(updatedRecord.Equals(controllerRecord));
                controller.AllLinkDatabase.ReplaceRecord(existingRecord, updatedRecord);
            }
        }
        else
        {
            // Add new record
            controller.AllLinkDatabase.AddRecord(controllerRecord);
        }
    }

    // Ensure that the controller link from a scene controller to a responder is present and correct
    private void EnsureResponderLinkRecord(SceneMember controllerMember, SceneMember responderMember)
    {
        var responder = House.Devices.GetDeviceByID(responderMember.DeviceId);
        if (responder == null)
        {
            // If the responder device does not exist, there is no link to create
            return;
        }

        if (responderMember.DeviceId == controllerMember.DeviceId)
        {
            if (responderMember.Group == controllerMember.Group)
            {
                // Controller and responder are the same, no need for a link
                return;
            }

            // Controller and responder are the same device but different groups, no controller link is needed
            // We just set the follow behavior on the responder keypad button, and level/ramp on the controller keypad button
            if (responder/*== controller*/.IsKeypadLinc)
            {
                EnsureResponderFollowBehavior(controllerMember, responderMember);
                return;
            }
        }

        var controller = House.Devices.GetDeviceByID(controllerMember.DeviceId);
        if (controller == null)
        {
            // If the controller device does not exist, there is no link to create
            return;
        }

        // Create responder link record
        AllLinkRecord responderRecord = new AllLinkRecord(controllerMember.DeviceId, isController: false, (byte)controllerMember.Group)
        {
            SceneId = this.Id,
            Data1 = responderMember.Data1,       // on level
            Data2 = responderMember.Data2,       // ramp rate
            Data3 = (byte)responderMember.Group, // responder device group/button
            SyncStatus = SyncStatus.Changed,
        };

        // Look for a match
        AllLinkRecord? existingRecord = null;
        if (responder.AllLinkDatabase.TryGetMatchingEntries(responderRecord, IdGroupTypeComparer.Instance, out List<AllLinkRecord>? matchingRecords))
        {
            foreach (AllLinkRecord record in matchingRecords)
            {
                if (record.Data3 == responderRecord.Data3)
                {
                    if (record.SceneId == 0 && existingRecord == null)
                    {
                        existingRecord = record;
                        break;
                    }
                    else if (record.SceneId == this.Id)
                    {
                        existingRecord = record;
                        break;
                    }
                }
            }
        }

        if (existingRecord != null)
        {
            if (!existingRecord.Equals(responderRecord))
            {
                // Update the existing record
                AllLinkRecord updatedRecord = new(existingRecord)
                {
                    Data1 = responderRecord.Data1,
                    Data2 = responderRecord.Data2,
                    Data3 = responderRecord.Data3,
                    SceneId = Id,
                    SyncStatus = SyncStatus.Changed,
                };        
                Debug.Assert(updatedRecord.Equals(responderRecord));
                responder.AllLinkDatabase.ReplaceRecord(existingRecord, updatedRecord);
            }
        }
        else
        {
            responder.AllLinkDatabase.AddRecord(responderRecord);
        }
    }

    // Ensure Responder Follow Behavior
    // On a KeypadLinc, ensure that the follow behavior of the responder button (channel) is set to obtain
    // the controller/responder behavior defined by the scene
    private void EnsureResponderFollowBehavior(SceneMember controllerMember, SceneMember responderMember)
    {
        // Controller and responder devices should be the same but the group should be different
        Debug.Assert(controllerMember.DeviceId == responderMember.DeviceId);
        Debug.Assert(controllerMember.Group != responderMember.Group);

        var device = House.Devices.GetDeviceByID(responderMember.DeviceId);
        Debug.Assert(device != null);   // if we got here, device should exist

        if (device.IsKeypadLinc && device.HasChannels)
        {
            Channel controllerChannel = device.GetChannel(controllerMember.Group);
            int bitMask = device.GetChannelBitMask(responderMember.Group);

            // Update Follow mask of controller channel if necessary
            if ((controllerChannel.FollowMask & bitMask) == 0)
            {
                controllerChannel.FollowMask |= bitMask;
            }

            // Data1 in the responder member is the level - If it is zero, follow off 
            // Update Follow Off mask of controller channel if necessary
            if (responderMember.Data1 == 0)
            {
                if ((controllerChannel.FollowOffMask & bitMask) == 0)
                {
                    controllerChannel.FollowOffMask |= bitMask;
                }
            }
            else
            {
                if ((controllerChannel.FollowOffMask & bitMask) != 0)
                {
                    controllerChannel.FollowOffMask &= ~bitMask;
                }

                // Set level and ramp rate (Data1/Data2 in responder member) on the controller button
                if (controllerChannel.OnLevel != responderMember.Data1 || controllerChannel.RampRate != responderMember.Data2)
                {
                    controllerChannel.OnLevel = responderMember.Data1;
                    controllerChannel.RampRate = responderMember.Data2;
                }
            }
        }
    }

    // Delete the controller link from a scene controller to a responder
    private void DeleteControllerLinkRecord(SceneMember controllerMember, SceneMember responderMember, bool removeOnlyDuplicates = false)
    {
        var controller = House.Devices.GetDeviceByID(controllerMember.DeviceId);

        if (controller == null)
        {
            // If the controller device does not exist, there is nothing to do
            return;
        }

        if (controllerMember.DeviceId == responderMember.DeviceId)
        {
            if (responderMember.Group == controllerMember.Group)
            {
                // Controller and responder are the same, no link to delete
                return;
            }

            // Controller and responder are the same device but different groups, no controller link was created
            if (controller.IsKeypadLinc)
            {
                return;
            }
        }

        // Create controller link to delete
        AllLinkRecord controllerRecord = new AllLinkRecord(responderMember.DeviceId, isController: true, (byte)controllerMember.Group)
        {
            SceneId = this.Id,
            Data1 = 0x03,                         // 3 retries
            Data2 = controllerMember.Data2,       // listed as unused but seems to be set to ramp rate
            Data3 = (byte)controllerMember.Group, // controller device group/button
        };

        // Look for matches
        List<AllLinkRecord> recordsToRemove = new();
        if (controller.AllLinkDatabase.TryGetMatchingEntries(controllerRecord, IdGroupTypeComparer.Instance, out List<AllLinkRecord>? matchingRecords))
        {
            foreach (AllLinkRecord record in matchingRecords)
            {
                // Remove only links attributed to this scene or not attributed
                if (record.SceneId == 0 || record.SceneId == this.Id)
                {
                    recordsToRemove.Add(record);
                }
            }
        }

        // Now delete the matching records, keeping one if removeOnlyDuplicates is true
        if (recordsToRemove.Count > 0)
        {
            for (var i = removeOnlyDuplicates ? 1 : 0; i < recordsToRemove.Count; i++)
            {
                if (removeOnlyDuplicates && i == 0)
                {
                    // Ensure the record we keep has the right scene id and is marked as changed
                    // so that it does not get changed or removed by the next sync
                    AllLinkRecord record = new(recordsToRemove[i]) { SceneId = Id, SyncStatus = SyncStatus.Changed };
                    controller.AllLinkDatabase.ReplaceRecord(recordsToRemove[i], record);
                }
                else
                {
                    // Need to create a new record here as it might not be the same duplicate that we are going to remove
                    AllLinkRecord record = new(recordsToRemove[i]) { SyncStatus = SyncStatus.Changed };
                    controller.AllLinkDatabase.RemoveRecord(record);
                }
            }
        }
    }

    // Delete the responder link from a scene responder to a controller
    private void DeleteResponderLinkRecord(SceneMember controllerMember, SceneMember responderMember, bool removeOnlyDuplicates = false)
    {
        var responder = House.Devices.GetDeviceByID(responderMember.DeviceId);

        if (responder == null)
        {
            // If the responder device does not exist, there is nothing to do
            return;
        }

        if (controllerMember.DeviceId == responderMember.DeviceId)
        {
            if (responderMember.Group == controllerMember.Group)
            {
                // Controller and responder are the same, there is no link to delete
                return;
            }

            // Controller and responder are the same device but different groups, so no responder link was created.
            // Instead follow behavior was set on the responder button. We need to clear it, unless we are only
            // removing duplicates, in which case, we keep things as they are.
            if (responder.IsKeypadLinc && !removeOnlyDuplicates)
            {
                // but we still need to clear the follow behavior on the responder button
                ClearResponderFollowBehavior(controllerMember, responderMember);
                return;
            }
        }

        // Create responder link to delete
        AllLinkRecord responderRecord = new AllLinkRecord(controllerMember.DeviceId, isController: false, (byte)controllerMember.Group)
        {
            SceneId = this.Id,
            Data1 = responderMember.Data1,       // on level
            Data2 = responderMember.Data2,       // ramp rate
            Data3 = (byte)responderMember.Group  // responder device group/button
        };

        // Look for a match
        List<AllLinkRecord> recordsToRemove = new();
        if (responder.AllLinkDatabase.TryGetMatchingEntries(responderRecord, IdGroupTypeComparer.Instance, out List<AllLinkRecord>? matchingRecords))
        {
            foreach (AllLinkRecord record in matchingRecords)
            {
                if (record.Data3 == responderRecord.Data3)
                {
                    // Remove only links attributed to this scene or not attributed
                    if (record.SceneId == 0 || record.SceneId == this.Id)
                    {
                        recordsToRemove.Add(record);
                    }
                }
            }
        }

        // Now delete the matching records, keeping one if removeOnlyDuplicates is true
        if (recordsToRemove.Count > 0)
        {
            for (var i = 0; i < recordsToRemove.Count; i++)
            {
                if (removeOnlyDuplicates && i == 0)
                {
                    // Ensure the record we keep has the right scene id and is marked as changed
                    // so that it does not get changed or removed by the next sync
                    AllLinkRecord record = new(recordsToRemove[i]) { SceneId = Id, SyncStatus = SyncStatus.Changed };
                    responder.AllLinkDatabase.ReplaceRecord(recordsToRemove[i], record);
                }
                else
                {
                    // Need to create a new record here as it might not be the same duplicate that we are going to remove
                    AllLinkRecord record = new(recordsToRemove[i]) { SyncStatus = SyncStatus.Changed };
                    responder.AllLinkDatabase.RemoveRecord(record);
                }
            }
        }
    }

    // Clear Responder Follow Behavior
    // On a KeypadLinc, clear the follow behavior of the responder button (channel) that was previously set for this scene
    private void ClearResponderFollowBehavior(SceneMember controllerMember, SceneMember responderMember)
    {
        // Controller and responder devices should be the same but the group should be different
        Debug.Assert(controllerMember.DeviceId == responderMember.DeviceId);
        Debug.Assert(controllerMember.Group != responderMember.Group);

        var device = House.Devices.GetDeviceByID(responderMember.DeviceId);
        Debug.Assert(device != null);   // if we got here, device should exist

        if (device.IsKeypadLinc && device.HasChannels)
        {
            Channel controllerChannel = device.GetChannel(controllerMember.Group);
            int bitMask = device.GetChannelBitMask(responderMember.Group);

            // Update Follow mask of responder channel if necessary
            if ((controllerChannel.FollowMask & bitMask) != 0)
            {
                controllerChannel.FollowMask &= ~bitMask;
            }

            // Data1 in the responder member is the level - If it is zero, follow off 
            // Update Follow Off mask of responder channel if necessary
            if (responderMember.Data1 == 0)
            {
                if ((controllerChannel.FollowOffMask & bitMask) != 0)
                {
                    controllerChannel.FollowOffMask &= ~bitMask;
                }
            }
        }
    }

    // Checks whether a scene really owns a given link record from a given device
    private bool SceneOwnsLinkRecord(InsteonID deviceId, AllLinkRecord record)
    {
        if (record.IsController)
        {
            if (!IsControllerMember(deviceId, record.Group))
            {
                return false;
            }
            if (!IsResponderMember(record.DestID, record.Data3))
            {
                return false;
            }
        }
        else
        {
            if (!IsResponderMember(deviceId, record.Data3))
            {
                return false;
            }
            if (!IsControllerMember(record.DestID, record.Group))
            {
                return false;
            }
        }

        return true;
    }

    // Check whether a given device/group is one of the controllers of this scene
    private bool IsControllerMember(InsteonID deviceId, int group)
    {
        foreach (SceneMember member in Members)
        {
            if (member.IsController && member.DeviceId == deviceId && member.Group == group)
            {
                return true;
            }
        }
        return false;
    }

    // Check whether a given device/group is one of the responders of this scene
    private bool IsResponderMember(InsteonID deviceId, int group)
    {
        foreach (SceneMember member in Members)
        {
            if (member.IsResponder && member.DeviceId == deviceId && member.Group == group)
            {
                return true;
            }
        }
        return false;
    }
}
