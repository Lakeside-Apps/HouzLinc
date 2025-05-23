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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Insteon.Model;

public sealed class SceneMembers : OrderedNonUniqueKeyedList<SceneMember>
{
    internal SceneMembers(Scene scene) : base()
    {
        Scene = scene;
    }

    public Scene Scene { get; init; }
    
    public House House => Scene.House;

    public SceneMembers(SceneMembers members) : this(members.Scene)
    {
        foreach (var member in members)
        {
            Add(new SceneMember(member));
        }
    }

    /// <summary>
    /// Member comparers to use with GetEntry and GetMatchingEntries
    /// </summary>
    class IdEqualityComparer : EqualityComparer<SceneMember>
    {
        public override bool Equals(SceneMember? x, SceneMember? y)
        {
            if (x == null || y == null) return false;
            return x.DeviceId == y.DeviceId;
        }

        public override int GetHashCode(SceneMember obj)
        {
            return obj.GetHashCode();
        }
    }

    class IdGroupEqualityComparer : EqualityComparer<SceneMember>
    {
        public override bool Equals(SceneMember? x, SceneMember? y)
        {
            if (x == null || y == null) return false;
            return x.DeviceId == y.DeviceId && x.Group == y.Group;
        }

        public override int GetHashCode(SceneMember obj)
        {
            return obj.GetHashCode();
        }
    }

    class IdGroupTypeEqualityComparer : EqualityComparer<SceneMember>
    {
        public override bool Equals(SceneMember? x, SceneMember? y)
        {
            if (x == null || y == null) return false;
            return x.DeviceId == y.DeviceId && x.Group == y.Group &&
                x.IsController == y.IsController && x.IsResponder == y.IsResponder;
        }

        public override int GetHashCode(SceneMember obj)
        {
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// Copy from another SceneMembers.
    /// </summary>
    /// <param name="fromMembers">See above</param>
    internal void CopyFrom(SceneMembers fromMembers)
    {
        // If the member lists are identical, nothing to do
        if (IsIdenticalTo(fromMembers))
        {
            return;
        }

        // Clear this list and bring the members in to notify the observers.
        // TODO: we could consider only copying the members that changed as
        // we do for Devices.CopyFrom, but it's unclear whether the added
        // complexity is worth it.
        Clear();
        foreach (var member in fromMembers)
        {
            Add(new SceneMember(member));
        }
    }

    /// <summary>
    /// Whether this is strictly identical to another set of scene members
    /// </summary>
    /// <param name="members"></param>
    /// <returns></returns>
    internal bool IsIdenticalTo(SceneMembers members)
    {
        if (Count != members.Count)
            return false;

        for (int i = 0; i < Count; i++)
        {
            if (!this[i].IsIdenticalTo(members[i])) return false;
        }
        return true;
    }

    internal void OnDeserialized()
    {
        AddObserver(House.ModelObserver);
    }

    /// <summary>
    /// Observers can subscribe to changes
    /// </summary>
    /// <param name="observer"></param>
    public SceneMembers AddObserver(ISceneMembersObserver observer)
    {
        observers?.Add(observer);
        return this;
    }
    private List<ISceneMembersObserver> observers = new List<ISceneMembersObserver>();

    /// <summary>
    /// Find member in a scene matching on id, group, type (controller/responder)
    /// </summary>
    /// <param name="member"></param>
    /// <param name="matchingMember"></param>
    /// <returns></returns>
    public bool TryGetMember(SceneMember member, [NotNullWhen(true)] out SceneMember? matchingMember)
    {
        return TryGetEntry(member, new IdGroupTypeEqualityComparer(), out matchingMember);
    }

    /// <summary>
    /// Find member in a scene matching id, group, type (controller/responder)
    /// </summary>
    /// <param name="insteonID"></param>
    /// <param name="group"></param>
    /// <param name="isController"></param>
    /// <param name="isResponder"></param>
    /// <param name="matchingMember"></param>
    /// <returns></returns>
    public bool TryGetMember(InsteonID insteonID, byte group, bool isController, bool isResponder, [NotNullWhen(true)] out SceneMember? matchingMember)
    {
        return TryGetEntry(new SceneMember(Scene)
        { 
            DeviceId = insteonID, 
            Group = group, 
            IsResponder = isResponder, 
            IsController = isController 
        },
        new IdGroupTypeEqualityComparer(), out matchingMember);
    }

    /// <summary>
    /// Find all members matching given id and group
    /// </summary>
    /// <param name="insteonID"></param>
    /// <param name="group"></param>
    /// <param name="matchingMembers"></param>
    /// <returns>true if at least one match is found</returns>
    public bool TryGetMatchingMembers(InsteonID insteonID, int group, [NotNullWhen(true)] out List<SceneMember>? matchingMembers)
    {
        return TryGetMatchingEntries(new SceneMember(Scene) { DeviceId = insteonID, Group = (byte)group }, new IdGroupEqualityComparer(), out matchingMembers);
    }

    /// <summary>
    /// Find controller members matching id
    /// </summary>
    /// <param name="insteonID"></param>
    /// <param name="matchingControllers"></param>
    /// <returns>true if at least one match is found</returns>
    public bool TryGetMatchingControllers(InsteonID insteonID, [NotNullWhen(true)] out List<SceneMember>? matchingControllers)
    {
        matchingControllers = null;
        if (TryGetMatchingEntries(new SceneMember(Scene) { DeviceId = insteonID }, new IdEqualityComparer(), out List<SceneMember>? matchingMembers))
        {
            matchingControllers = new List<SceneMember>();
            foreach (var member in matchingMembers!)
            {
                if (member.IsController) matchingControllers.Add(member);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Find responder members matching id
    /// </summary>
    /// <param name="insteonID"></param>
    /// <param name="matchingResponders"></param>
    /// <returns></returns>
    public bool TryGetMatchingResponders(InsteonID insteonID, [NotNullWhen(true)] out List<SceneMember>? matchingResponders)
    {
        matchingResponders = null;
        if (TryGetMatchingEntries(new SceneMember(Scene) { DeviceId = insteonID }, new IdEqualityComparer(), out List<SceneMember>? matchingMembers))
        {
            matchingResponders = new List<SceneMember>();
            foreach (var member in matchingMembers)
            {
                if (member.IsResponder) matchingResponders.Add(member);
            }
            return true;
        }
        return false;
    }

    protected override void ClearItems()
    {
        base.ClearItems();
        observers.ForEach(o => o.SceneMembersCleared(Scene));
    }

    protected override void SetItem(int index, SceneMember item)
    {
        var itemToReplace = this[index];
        base.SetItem(index, item);
        observers.ForEach(o => o.SceneMemberReplaced(Scene, itemToReplace, item));
    }

    protected override void InsertItem(int index, SceneMember item)
    {
        Debug.Assert(index == Count, "Only adding a member at the end of a scene is supported");
        base.InsertItem(index, item);
        observers.ForEach(o => o.SceneMemberAdded(Scene, item));
    }

    protected override void RemoveItem(int index)
    {
        var itemToRemove = this[index];
        base.RemoveItem(index);
        observers.ForEach(o => o.SceneMemberRemoved(Scene, itemToRemove));
    }
}
