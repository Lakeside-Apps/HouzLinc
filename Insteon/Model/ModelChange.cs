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
using Insteon.Base;
using static Insteon.Model.AllLinkRecord;

namespace Insteon.Model;

/// <summary>
/// Base class for a recorded and replayable change to the model
/// </summary>
internal abstract class ModelChange
{
    internal abstract void Apply(House house);
}

/// <summary>
/// The following classes all derive from ModelChange and represent a specific change to the model
/// which is recorded by the ModelObserver and can be played back by the ModelChangePlayer
/// </summary>

internal class GatewayChangedChange : ModelChange
{
    internal GatewayChangedChange(Gateway gateway)
    {
        this.macAddress = gateway.MacAddress;
        this.hostName = gateway.HostName;
        this.ipAddress = gateway.IPAddress;
        this.port = gateway.Port;
        this.deviceId = gateway.DeviceId;
    }

    internal override void Apply(House house)
    {
        house.PushNewGateway(new Gateway(house, macAddress, hostName, ipAddress, port) { DeviceId = deviceId });
    }

    string macAddress;
    string hostName;
    string ipAddress;
    string port;
    InsteonID deviceId;
}

internal class DeviceAddedChange : ModelChange
{
    internal DeviceAddedChange(Device device)
    {
        this.deviceId = device.Id;
        this.addedDateTime = device.AddedDateTime;

        // Ensure these properties have not been set yet.
        // They should be set after the device is added to generate property change deltas.
        Debug.Assert(device.CategoryId == default(DeviceKind.CategoryId));
        Debug.Assert(device.SubCategory == default);
        Debug.Assert(device.Revision == default);
        Debug.Assert(device.Status == default);
    }

    internal override void Apply(House house)
    {
        try
        {
            house.Devices.Add(new Device(house.Devices, deviceId)
            {
                AddedDateTime = addedDateTime
            });
        }
        catch (ArgumentException)
        {
            // It's possible that the device has already been added by another running
            // instance of the app, in that case, fail silently.
        }
    }

    InsteonID deviceId;
    DateTime addedDateTime;
}

internal class DeviceInsertedChange : ModelChange
{
    internal DeviceInsertedChange(int seq, Device device)
    {
        // At the moment this only works for seq == 0
        Debug.Assert(seq == 0);

        this.deviceId = device.Id;
        this.addedDateTime = device.AddedDateTime;
        this.seq = seq;

        // Ensure these properties have not been set yet.
        // They should be set after the device is added to generate property change deltas.
        Debug.Assert(device.CategoryId == default(DeviceKind.CategoryId));
        Debug.Assert(device.SubCategory == default);
        Debug.Assert(device.Revision == default);
        Debug.Assert(device.Status == default);
    }

    internal override void Apply(House house)
    {
        house.Devices.Insert(seq, new Device(house.Devices, deviceId)
        {
            AddedDateTime = addedDateTime
        });
    }

    InsteonID deviceId;
    DateTime addedDateTime;
    int seq;
}

internal class DeviceRemovedChange : ModelChange
{
    internal DeviceRemovedChange(Device device)
    {
        this.deviceId = device.Id;
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null) house.Devices.Remove(device);
    }

    InsteonID deviceId;
}

internal class DevicePropertyChangedChange : ModelChange
{
    internal DevicePropertyChangedChange(Device device, string propertyName)
    {
        this.deviceId = device.Id;
        this.propertyName = propertyName;
    
        this.propertyValue = device.GetType().GetProperty(propertyName)?.GetValue(device);
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null)
        {
            device.GetType()?.GetProperty(propertyName)?.SetValue(device, propertyValue);
        }
    }

    InsteonID deviceId;
    string propertyName;
    object? propertyValue;
}

internal class DevicePropertiesSyncStatusChanged : ModelChange
{
    internal DevicePropertiesSyncStatusChanged(Device device)
    {
        this.deviceId = device.Id;
        this.status = device.PropertiesSyncStatus;
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null)
        {
            device.PropertiesSyncStatus = status;
        }
    }

    InsteonID deviceId;
    SyncStatus status;
}

internal class DeviceChannelsChangedChange : ModelChange
{
    internal DeviceChannelsChangedChange(Device device)
    {
        this.deviceId = device.Id;
        // Clone channels to insulate from further changes
        this.channels = new Channels(device.Channels);
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null)
        {
            device.Channels = channels;
        }
    }

    InsteonID deviceId;
    Channels channels;
}

internal class ChannelPropertyChangedChange : ModelChange
{
    internal ChannelPropertyChangedChange(Channel channel, string propertyName)
    {
        this.deviceId = channel.Device.Id;
        this.channelId = channel.Id;
        this.propertyName = propertyName;
        this.propertyValue = channel.GetType().GetProperty(propertyName)?.GetValue(channel) ?? null;
        this.propertyLastUpdate = (channel.GetType().GetProperty(propertyName + "LastUpdate")?.GetValue(channel) ?? null) as DateTime? ?? DateTime.MinValue;
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null)
        {
            var channel = device.GetChannel(channelId);
            if (channel != null)
            {
                channel.GetType().GetProperty(propertyName)?.SetValue(channel, propertyValue, null);
                channel.GetType().GetProperty(propertyName + "LastUpdate")?.SetValue(channel, propertyLastUpdate, null);
            }
        }
    }

    InsteonID deviceId;
    int channelId;
    string propertyName;
    object? propertyValue;
    DateTime propertyLastUpdate;
}

internal class ChannelSyncStatusChangedChange : ModelChange
{
    internal ChannelSyncStatusChangedChange(Channel channel)
    {
        this.deviceId = channel.Device.Id;
        this.channelId = channel.Id;
        this.status = channel.PropertiesSyncStatus;
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null)
        {
            var channel = device.GetChannel(channelId);
            if (channel != null)
            {
                channel.PropertiesSyncStatus = status;
            }
        }
    }

    InsteonID deviceId;
    int channelId;
    SyncStatus status;
}

internal class AllLinkDatabaseChangedChange : ModelChange
{
    internal AllLinkDatabaseChangedChange(Device device)
    {
        this.deviceId = device.Id;
        this.allLinkDatabase = new AllLinkDatabase(device.AllLinkDatabase);
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null)
        {
            device.AllLinkDatabase = allLinkDatabase;
        }
    }

    InsteonID deviceId;
    AllLinkDatabase allLinkDatabase;
}

internal class AllLinkDatabaseSyncStatusChangedChange : ModelChange
{
    internal AllLinkDatabaseSyncStatusChangedChange(Device device)
    {
        this.deviceId = device.Id;
        this.status = device.AllLinkDatabase.LastStatus;
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null)
        {
            device.AllLinkDatabase.LastStatus = status;
        }
    }

    InsteonID deviceId;
    SyncStatus status;
}

internal class AllLinkDatabasePropertiesChangedChange : ModelChange
{
    internal AllLinkDatabasePropertiesChangedChange(Device device)
    {
        this.deviceId = device.Id;
        this.nextRecordToRead = device.AllLinkDatabase.NextRecordToRead;
        this.revision = device.AllLinkDatabase.Revision;
        this.lastUpdate = device.AllLinkDatabase.LastUpdate;
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null && device.AllLinkDatabase != null)
        {
            device.AllLinkDatabase.NextRecordToRead = nextRecordToRead;
            device.AllLinkDatabase.Revision = revision;
            device.AllLinkDatabase.LastUpdate = lastUpdate;
        }
    }

    InsteonID deviceId;
    int nextRecordToRead;
    int revision;
    DateTime lastUpdate;
}

internal class AllLinkDatabaseClearedChange : ModelChange
{
    internal AllLinkDatabaseClearedChange(Device device)
    {
        this.deviceId = device.Id;
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null && device.AllLinkDatabase != null)
        {
            device.AllLinkDatabase.Clear();
        }
    }

    InsteonID deviceId;
}

internal class AllLinkRecordAddedChange : ModelChange
{
    internal AllLinkRecordAddedChange(Device device, AllLinkRecord record)
    {
        this.deviceId = device.Id;
        this.record = record;
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null && device.AllLinkDatabase != null)
        {
            // Avoid creating duplicate records
            if (!device.AllLinkDatabase.TryGetEntry(record, comparer: null, out _))
            {
                device.AllLinkDatabase.Add(record);
            }
        }
    }

    InsteonID deviceId;
    AllLinkRecord record;
}

internal class AllLinkRecordRemovedChange : ModelChange
{
    internal AllLinkRecordRemovedChange(Device device, AllLinkRecord record)
    {
        this.deviceId = device.Id;
        this.recordUid = record.Uid;
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null && device.AllLinkDatabase != null)
        {
            device.AllLinkDatabase.Remove(recordUid);
        }
    }

    InsteonID deviceId;
    uint recordUid;
}

internal class AllLinkRecordReplacedChange : ModelChange
{
    internal AllLinkRecordReplacedChange(Device device, AllLinkRecord recordToReplace, AllLinkRecord newRecord)
    {
        this.deviceId = device.Id;
        // No need to make a copy, AllLinkRecord is immutable,
        this.recordToReplaceUid = recordToReplace.Uid;
        this.newRecord = newRecord;
    }

    internal override void Apply(House house)
    {
        var device = house.Devices.GetDeviceByID(deviceId);
        if (device != null && device.AllLinkDatabase != null)
        {
            device.AllLinkDatabase.Replace(recordToReplaceUid, newRecord);
        }
    }

    InsteonID deviceId;
    uint recordToReplaceUid;
    AllLinkRecord newRecord;
}

internal class SceneMembersClearedChange : ModelChange
{
    internal SceneMembersClearedChange(Scene scene)
    {
        this.sceneId = scene.Id;
    }

    internal override void Apply(House house)
    {
        var scene = house.Scenes.GetSceneById(sceneId);
        if (scene != null)
        {
            scene.RemoveAllMembers();
        }
    }

    int sceneId;
}

internal class SceneMemberAddedChange : ModelChange
{
    internal SceneMemberAddedChange(Scene scene, SceneMember member)
    {
        this.sceneId = scene.Id;
        this.member = new SceneMember(member);
    }

    internal override void Apply(House house)
    {
        if (house.Scenes.TryGetEntry(sceneId, out var scene))
        {
            scene.Members.Add(member);
        }
    }

    int sceneId;
    SceneMember member;
}

internal class SceneMemberReplacedChange : ModelChange
{
    internal SceneMemberReplacedChange(Scene scene, SceneMember memberToReplace, SceneMember newMember)
    {
        this.sceneId = scene.Id;

        // SceneMember being immutable, no need to make copies
        this.memberToReplace = memberToReplace;
        this.newMember = newMember;
    }

    internal override void Apply(House house)
    {
        if (house.Scenes.TryGetEntry(sceneId, out var scene))
        {
            var index = scene.Members.IndexOf(memberToReplace);
            scene.Members[index] = newMember;
        }
    }

    int sceneId;
    SceneMember memberToReplace;
    SceneMember newMember;
}

internal class SceneMemberRemovedChange : ModelChange
{
    internal SceneMemberRemovedChange(Scene scene, SceneMember member)
    {
        this.sceneId = scene.Id;
        this.member = member;
    }

    internal override void Apply(House house)
    {
        if (house.Scenes.TryGetEntry(sceneId, out var scene))
        {
            scene.Members.Remove(member);
        }
    }

    int sceneId;
    SceneMember member;
}

internal class ScenePropertyChangedChange : ModelChange
{
    internal ScenePropertyChangedChange(Scene scene, string propertyName)
    {
        this.sceneId = scene.Id;
        this.propertyName = propertyName;
        this.propertyValue = scene.GetType().GetProperty(propertyName)?.GetValue(scene) ?? null;
    }

    internal override void Apply(House house)
    {
        if (house.Scenes.TryGetEntry(sceneId, out var scene))
        {
            scene.GetType().GetProperty(propertyName)?.SetValue(scene, propertyValue, null);
        }
    }

    int sceneId;
    string propertyName;
    object? propertyValue;
}

internal class SceneMembersChangedChange : ModelChange
{
    internal SceneMembersChangedChange(Scene scene, SceneMembers members)
    {
        this.sceneId = scene.Id;
        this.members = new SceneMembers(members);
    }

    internal override void Apply(House house)
    {
        if (house.Scenes.TryGetEntry(sceneId, out var scene))
        {
            scene.Members = members;
        }
    }

    int sceneId;
    SceneMembers members;
}

internal class SceneAddedChange : ModelChange
{
    internal SceneAddedChange(Scene scene)
    {
        // For now, this should only be called for newly created scenes
        Debug.Assert(scene.Members == null || scene.Members.Count == 0);
        this.sceneId = scene.Id;
        this.sceneName = scene.Name;
    }

    internal override void Apply(House house)
    {
        house.Scenes.Add(new Scene(house.Scenes, sceneName, sceneId));
    }

    int sceneId;
    string sceneName;
}

internal class SceneRemovedChange : ModelChange
{
    internal SceneRemovedChange(Scene scene)
    {
        this.sceneId = scene.Id;
    }

    internal override void Apply(House house)
    {
        var scene = house.Scenes.GetSceneById(sceneId);
        if (scene != null)
        {
            house.Scenes.Remove(scene);
        }
    }

    int sceneId;
}

internal class ScenesPropertyChangedChange : ModelChange
{
    internal ScenesPropertyChangedChange(Scenes scenes)
    {
        this.nextSceneId = scenes.NextSceneID;
    }

    internal override void Apply(House house)
    {
        house.Scenes.NextSceneID = nextSceneId;
    }

    int nextSceneId;
}
