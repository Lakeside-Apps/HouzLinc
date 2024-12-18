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
using Insteon.Base;

namespace Insteon.Model;

/// <summary>
/// This class observes changes in the model (house configuration), except during deserialization.
/// It is instantiated by the House class (the root of the model). It collects each model change
/// in a queue in order to handle merges when multiple instances of this application work concurrently 
/// on the same model file. Merges are achieved by replaying changes to the model since the last time 
/// it was persisted against the lastest persisted model (possibly modified by other instances of the app).
/// In addition, subscribers of OnModelChanged will get notified of changes.
/// </summary>
public sealed class ModelObserver : 
    IAllLinkDatabaseObserver, IChannelObserver, IDevicesObserver, IDeviceObserver,
    ISceneMembersObserver, ISceneObserver, IScenesObserver, IGatewaysObserver
{
    internal ModelObserver(ModelRecorder player)
    {
        modelChangePlayer = player;
    }

    private ModelRecorder modelChangePlayer;

    public event Action? OnModelChanged;
    public event Action? OnModelNeedsSync;

    internal void NotifyModelNeedsSync()
    {
        OnModelNeedsSync?.Invoke();
    }

    void IGatewaysObserver.GatewayChanged(Gateway newGateway)
    {
        modelChangePlayer.Record(new GatewayChangedChange(newGateway));
        OnModelChanged?.Invoke();
    }

    void IDevicesObserver.DeviceAdded(Device device)
    {
        modelChangePlayer.Record(new DeviceAddedChange(device));
        OnModelChanged?.Invoke();
    }

    void IDevicesObserver.DeviceInserted(int seq, Device device)
    {
        modelChangePlayer.Record(new DeviceInsertedChange(seq, device));
        OnModelChanged?.Invoke();
    }

    void IDevicesObserver.DeviceRemoved(Device device)
    {
        modelChangePlayer.Record(new DeviceRemovedChange(device));
        OnModelChanged?.Invoke();
    }

    void IDeviceObserver.DevicePropertyChanged(Device device, string? propertyName)
    {
        if (propertyName == null)
            return;

        modelChangePlayer.Record(new DevicePropertyChangedChange(device, propertyName));
        OnModelChanged?.Invoke();
    }

    void IDeviceObserver.DevicePropertiesSyncStatusChanged(Device device)
    {
        modelChangePlayer.Record(new DevicePropertiesSyncStatusChanged(device));
        if (device.PropertiesSyncStatus == SyncStatus.Changed) 
            OnModelNeedsSync?.Invoke();
        OnModelChanged?.Invoke();
    }

    void IDeviceObserver.DeviceChannelsChanged(Device device)
    {
        modelChangePlayer.Record(new DeviceChannelsChangedChange(device));
        OnModelChanged?.Invoke();
    }

    void IChannelObserver.ChannelPropertyChanged(Channel channel, string? propertyName)
    {
        if (propertyName == null)
            return;

        modelChangePlayer.Record(new ChannelPropertyChangedChange(channel, propertyName));
        if (channel.PropertiesSyncStatus == SyncStatus.Changed)
            OnModelNeedsSync?.Invoke();
        OnModelChanged?.Invoke();
    }

    void IChannelObserver.ChannelSyncStatusChanged(Channel channel)
    {
        modelChangePlayer.Record(new ChannelSyncStatusChangedChange(channel));
        if (channel.PropertiesSyncStatus == SyncStatus.Changed)
            OnModelNeedsSync?.Invoke();
        OnModelChanged?.Invoke();
    }

    void IDeviceObserver.AllLinkDatabaseChanged(Device? device, AllLinkDatabase allLinkDatabase)
    {
        if (device == null)
            return;

        modelChangePlayer.Record(new AllLinkDatabaseChangedChange(device));
        OnModelChanged?.Invoke();
    }

    void IAllLinkDatabaseObserver.AllLinkDatabaseSyncStatusChanged(Device? device)
    {
        if (device == null)
            return;

        modelChangePlayer.Record(new AllLinkDatabaseSyncStatusChangedChange(device));
        if (device.AllLinkDatabase.LastStatus == SyncStatus.Changed)
            OnModelNeedsSync?.Invoke();
        OnModelChanged?.Invoke();
    }

    void IAllLinkDatabaseObserver.AllLinkDatabasePropertiesChanged(Device? device)
    {
        if (device == null)
            return;

        modelChangePlayer.Record(new AllLinkDatabasePropertiesChangedChange(device));
        OnModelChanged?.Invoke();
    }

    void IAllLinkDatabaseObserver.AllLinkDatabaseCleared(Insteon.Model.Device? device)
    {
        if (device == null)
            return;

        modelChangePlayer.Record(new AllLinkDatabaseClearedChange(device));
        OnModelChanged?.Invoke();
    }

    void IAllLinkDatabaseObserver.AllLinkRecordAdded(Device? device, AllLinkRecord record)
    {
        if (device == null)
            return;

        modelChangePlayer.Record(new AllLinkRecordAddedChange(device, record));
        OnModelChanged?.Invoke();
    }

    void IAllLinkDatabaseObserver.AllLinkRecordRemoved(Device? device, AllLinkRecord record)
    {
        if (device == null)
            return;

        modelChangePlayer.Record(new AllLinkRecordRemovedChange(device, record));
        OnModelChanged?.Invoke();
    }

    void IAllLinkDatabaseObserver.AllLinkRecordReplaced(Device? device, AllLinkRecord recordToReplace, AllLinkRecord newRecord)
    {
        if (device == null)
            return;

        modelChangePlayer.Record(new AllLinkRecordReplacedChange(device, recordToReplace, newRecord));
        OnModelChanged?.Invoke();
    }

    void ISceneMembersObserver.SceneMembersCleared(Scene scene)
    {
        modelChangePlayer.Record(new SceneMembersClearedChange(scene));
        OnModelChanged?.Invoke();
    }

    void ISceneMembersObserver.SceneMemberAdded(Scene scene, SceneMember sceneMember)
    {
        modelChangePlayer.Record(new SceneMemberAddedChange(scene, sceneMember));
        OnModelChanged?.Invoke();
    }

    void ISceneMembersObserver.SceneMemberReplaced(Scene scene, SceneMember memberToReplace, SceneMember newMember)
    {
        modelChangePlayer.Record(new SceneMemberReplacedChange(scene, memberToReplace, newMember));
        OnModelChanged?.Invoke();
    }

    void ISceneMembersObserver.SceneMemberRemoved(Scene scene, SceneMember member)
    {
        modelChangePlayer.Record(new SceneMemberRemovedChange(scene, member));
        OnModelChanged?.Invoke();
    }

    void ISceneObserver.ScenePropertyChanged(Scene scene, string? propertyName)
    {
        if (propertyName == null)
            return;

        modelChangePlayer.Record(new ScenePropertyChangedChange(scene, propertyName));
        OnModelChanged?.Invoke();
    }

    void ISceneObserver.SceneMembersChanged(Scene scene, SceneMembers members)
    {
        modelChangePlayer.Record(new SceneMembersChangedChange(scene, members));
        OnModelChanged?.Invoke();
    }

    void IScenesObserver.SceneAdded(Scene scene)
    {
        modelChangePlayer.Record(new SceneAddedChange(scene));
        OnModelChanged?.Invoke();
    }

    void IScenesObserver.SceneRemoved(Scene scene)
    {
        modelChangePlayer.Record(new SceneRemovedChange(scene));
        OnModelChanged?.Invoke();
    }

    void IScenesObserver.ScenesPropertyChanged(Scenes scenes)
    {
        modelChangePlayer.Record(new ScenesPropertyChangedChange(scenes));
        OnModelChanged?.Invoke();
    }
}
