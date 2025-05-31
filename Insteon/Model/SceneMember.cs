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

namespace Insteon.Model;

public sealed class SceneMember
{
    public SceneMember(Scene scene)
    {
        this.scene = scene;
    }

    public Scene scene { get; init; }

    public House House => scene.House;

    public SceneMember(SceneMember member)
    {
        this.scene = member.scene;
        this.DeviceId = member.DeviceId;
        this.Group = member.Group;
        this.IsController = member.IsController;
        this.IsResponder = member.IsResponder;
        this.Data1 = member.Data1;
        this.Data2 = member.Data2;
        this.Data3 = member.Data3;
        this.Status = member.Status;
        this.Tag = member.Tag;
    }

    public override int GetHashCode()
    {
        return DeviceId.ToInt();
    }
    
    /// <summary>
    /// Wether this member is the same as another.
    /// Ignores some deprecated fields.
    /// TODO: consider making the same as IsIdenticalTo.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj is SceneMember sceneMember)
        {
            return DeviceId == sceneMember.DeviceId &&
                Group == sceneMember.Group &&
                IsController == sceneMember.IsController &&
                IsResponder == sceneMember.IsResponder &&
                Data1 == sceneMember.Data1 &&
                Data2 == sceneMember.Data2 &&
                Data3 == sceneMember.Data3;
        }
        return false;
    }

    // TODO: consider implementing the equality operator

    /// <summary>
    /// Wether this member is strictly identical to another
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    internal bool IsIdenticalTo(SceneMember member)
    {
        return 
            this.Equals(member) &&
            this.Status == member.Status &&
            this.Tag == member.Tag;
    }

    public InsteonID DeviceId { get; init; } = default!;

    public byte Group { get; init; }

    public bool IsController { get; init; }

    public bool IsResponder { get; init; }

    public byte Data1 { get; init; }

    public byte Data2 { get; init; }

    public byte Data3 { get; init; }

    // Aliases for light dimming devices
    public int OnLevel { get => Data1; init => Data1 = (byte)value; }
    public int RampRate { get => Data2; init => Data2 = (byte)value; }

    // Not Used at this time. Only here to round-trip back to houselinc.xml
    public int Tag { get; init; }
    public string Status { get; init; } = null!;
}
