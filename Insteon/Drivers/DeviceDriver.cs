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
using Insteon.Commands;
using System.Diagnostics;

namespace Insteon.Drivers;

// Driver of a physical device on the network
// It caches some of the device state and provides facilities to read 
// and write the state of the physical device
internal class DeviceDriver : DeviceDriverBase
{
    internal DeviceDriver(House house, InsteonID Id) : base(house, Id)
    {
    }

    internal DeviceDriver(Device device) : base(device)
    {
    }

    /// <summary>
    /// Number of channels and id of first channel
    /// </summary>
    internal override int ChannelCount => 1;
    internal override int FirstChannelId => 1;

    /// <summary>
    /// List of physical channels
    /// </summary>
    internal ChannelDriver[]? Channels
    {
        get
        {
            // We only build a list of channels for devices with more than one channel
            if (ChannelCount > 1 && channels == null)
                channels = new ChannelDriver[ChannelCount];
            return channels;
        }
    }
    private ChannelDriver[]? channels;

    /// <summary>
    /// Get channel default name
    /// </summary>
    /// <param name="channelId"></param>
    /// <returns></returns>
    internal override string? GetChannelDefaultName(int channelId) { return string.Empty; }

    /// <summary>
    /// Ping the device and return its connection status.
    /// To create a quick way to check if the device is reachable, we only try once.
    /// Callers should explicitely run the command again if needed.
    /// </summary>
    /// <returns>whether it is</returns>
    internal async override Task<Device.ConnectionStatus> TryPingAsync(int maxAttempts = 1)
    {
        var command = new PingCommand(House.Gateway, Id) { MockPhysicalDevice = MockPhysicalDevice };
        if (await command.TryRunAsync(maxAttempts: maxAttempts))
        {
            return Device.ConnectionStatus.Connected;
        }

        if (command.ErrorReason == Command.ErrorReasons.TransientHttpError)
        {
            return Device.ConnectionStatus.Unknown;
        }

        if (command.ErrorReason == Command.ErrorReasons.HttpRequestError ||
            command.ErrorReason == Command.ErrorReasons.Timeout)
        {
            return Device.ConnectionStatus.GatewayError;
        }

        return Device.ConnectionStatus.Disconnected;
    }

    /// <summary>
    /// Try cleaning up after sync.
    /// Currently only used for remotelinc. Fail silently for other devices.
    /// </summary>
    /// <returns></returns>
#pragma warning disable CS1998
    internal async override Task<bool> TryCleanUpAfterSyncAsync()
#pragma warning restore CS1998
    {
        return true;
    }

    /// <summary>
    /// Turn light device on at specified level
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    internal override async Task<bool> TryLightOn(double level)
    {
        int levelInt = (int)(level * 255);
        if (levelInt < 0) levelInt = 0;
        if (levelInt > 255) levelInt = 255;
        var command = new LightOnCommand(House.Gateway, Id, (byte)levelInt);
        return await command.TryRunAsync();
    }

    /// <summary>
    /// Turn light device off at specified level
    /// </summary>
    /// <returns></returns>
    internal override async Task<bool> TryLightOff()
    {
        var command = new LightOffCommand(House.Gateway, Id);
        return await command.TryRunAsync();
    }

    /// <summary>
    /// Return the current on-level of the device load
    /// </summary>
    /// <returns>On level (0F-100F)</returns>
    internal override async Task<double> TryGetLightOnLevel()
    {
        var command = new GetOnLevelCommand(House.Gateway, Id);
        if (await command.TryRunAsync())
        {
            return command.OnLevel / 255.0;
        }
        return -1.0D;
    }

    /// <summary>
    /// Flags indicating where properties have been read from the physical device
    /// </summary>
    internal bool ArePropertiesRead =>
        (!HasOperatingFlags || AreOperatingFlagsRead) &&
        (!HasOtherProperties || AreOtherPropertiesRead);

    // Components making up ArePropertiesRead
    private protected bool AreOperatingFlagsRead;
    private protected bool IsLEDBrightnessRead;
    private protected bool IsOnLevelRead;
    private protected bool IsRampRateRead;
    private protected bool AreOtherPropertiesRead => IsLEDBrightnessRead && IsOnLevelRead && IsRampRateRead;

    /// <summary>
    /// Read device operating flags
    /// </summary>
    /// <param name="force">whether force a new read from the network even if we already read previously</param>
    /// <returns></returns>
    internal async override Task<bool> TryReadOperatingFlagsAsync(bool force = false)
    {
        bool success = true;

        // We will need to handle the IM separately with the GetIMInfo command
        Debug.Assert(GetType() != typeof(IMDriver));

        if (!AreOperatingFlagsRead || force)
        {
            success = false;

            var command = new GetOperatingFlagsCommand(House.Gateway, Id) { MockPhysicalDevice = House.GetMockPhysicalDevice(Id) };
            if (await command.TryRunAsync())
            {
                this.OperatingFlags = command.OperatingFlags;
                this.AreOperatingFlagsRead = true;
                success = true;
            }
        }

        return success;
    }

    /// <summary>
    /// Try to write Operating Flags to the device, if different.
    /// </summary>
    /// <param name="operatingFlags">Operating flags to write</param>
    /// <param name="opFlags2">Operating flags to write, second byte. Only used by derived classes.</param>
    /// <returns>true on success</returns>
    internal async override Task<bool> TryWriteOperatingFlagsAsync(Bits operatingFlags, Bits opFlags2)
    {
        bool success = false;

        // TODO: we need to handle IM separately with SetIMConfirmation command
        Debug.Assert(GetType() != typeof(IMDriver));

        // Ensure flags have been read into this device driver
        // so that we can just write the diffs
        await TryReadOperatingFlagsAsync(false);

        if (AreOperatingFlagsRead)
        {
            success = true;

            // Then write the flags that are different
            for (int i = 0; i < 8; i++)
            {
                if (operatingFlags[i] != OperatingFlags[i])
                {
                    var command = new SetOperatingFlagCommand(House.Gateway, Id, (byte)(operatingFlags[i] ? i * 2 : i * 2 + 1));
                    if (!await command.TryRunAsync(maxAttempts: 15))
                    {
                        success = false;
                        break;
                    }
                }
            }
        }

        if (success)
        {
            OperatingFlags = operatingFlags;
        }

        return success;
    }

    /// <summary>
    /// Try to read overall LED brightness (1-127) from the device
    /// </summary>
    /// <returns>success</returns>
    internal async override Task<bool> TryReadLEDBrightnessAsync()
    {
        // The IM does not support LED brightness
        Debug.Assert(GetType() != typeof(IMDriver));

        bool success = true;
        if (!IsLEDBrightnessRead)
        {
            success = false;

            // For no-channel devices, we read the properties for channel/group 1.
            // For multi-channel devices, we could read it from any channel/group, so we use group 1.
            var command = new GetPropertiesForGroupCommand(House.Gateway, Id, group: 1) { MockPhysicalDevice = House.GetMockPhysicalDevice(Id) };
            if (await command.TryRunAsync())
            {
                this.LEDBrightness = command.LEDBrightness;
                this.IsLEDBrightnessRead = true;
                success = true;
            }
        }

        return success;
    }

    /// <summary>
    /// Try to write the overall LED brightness to the device, if different.
    /// </summary>
    /// <param name="brightness">Desired brightness (1-127)</param>
    /// <returns>true on success</returns>
    internal async override Task<bool> TryWriteLEDBrightnessAsync(byte brightness)
    {
        // TODO: the IM does not support LED brightness
        Debug.Assert(GetType() != typeof(IMDriver));

        bool success = true; 
        if (!await TryReadLEDBrightnessAsync() || brightness != LEDBrightness)
        {
            success = false;
            var command = new SetLEDBrightnessCommand(House.Gateway, Id, brightness) { MockPhysicalDevice = House.GetMockPhysicalDevice(Id) };
            if (await command.TryRunAsync(maxAttempts: 15))
            {
                this.LEDBrightness = brightness;
                this.IsLEDBrightnessRead = true;
                success = true;
            }
        }

        return success;
    }

    /// <summary>
    /// Try to read OnLevel from the device
    /// For multi-channel devices, this returns properties for channel/group 1.
    /// </summary>
    /// <returns>success</returns>
    internal async override Task<bool> TryReadOnLevelAsync()
    {
        bool success = true;
        if (!IsOnLevelRead)
        {
            success = false;

            // This command works on no-channel devices, using channel/group 1.
            var command = new GetPropertiesForGroupCommand(House.Gateway, Id, group: 1) { MockPhysicalDevice = House.GetMockPhysicalDevice(Id) };
            if (await command.TryRunAsync())
            {
                this.OnLevel = command.OnLevel;
                this.IsOnLevelRead = true;
                success = true;
            }
        }

        return success;
    }

    /// <summary>
    /// Try to write the OnLevel to the device
    /// </summary>
    /// <param name="onLevel">Desired OnLevel (1-255)</param>
    /// <returns>true on success</returns>
    internal async override Task<bool> TryWriteOnLevelAsync(byte onLevel)
    {
        bool success = true;
        if (!await TryReadOnLevelAsync() || onLevel != OnLevel)
        {
            success = false;
            var command = new SetOnLevelForGroupCommand(House.Gateway, Id, group: 1, onLevel) { MockPhysicalDevice = House.GetMockPhysicalDevice(Id) };
            if (await command.TryRunAsync(maxAttempts: 15))
            {
                this.OnLevel = onLevel;
                this.IsOnLevelRead = true;
                success = true;
            }
        }

        return success;
    }

    /// <summary>
    /// Try to read OnLevel from the device
    /// For multi-channel devices, this returns properties for channel/group 1.
    /// </summary>
    /// <returns>success</returns>
    internal async override Task<bool> TryReadRampRateAsync()
    {
        bool success = true;
        if (!IsRampRateRead)
        {
            success = false;

            // This command works on no-channel devices, using channel/group 1.
            var command = new GetPropertiesForGroupCommand(House.Gateway, Id, group: 1) { MockPhysicalDevice = House.GetMockPhysicalDevice(Id) };
            if (await command.TryRunAsync())
            {
                this.RampRate = command.RampRate;
                this.IsRampRateRead = true;
                success = true;
            }
        }

        return success;
    }

    /// <summary>
    /// Try to write the OnLevel to the device
    /// </summary>
    /// <param name="rampRate">Desired ramp rate (0-31)</param>
    /// <returns>true on success</returns>
    internal async override Task<bool> TryWriteRampRateAsync(byte rampRate)
    {
        bool success = true;
        if (!await TryReadRampRateAsync() || rampRate != RampRate)
        {
            success = false;
            var command = new SetRampRateForGroupCommand(House.Gateway, Id, group: 1, rampRate) { MockPhysicalDevice = House.GetMockPhysicalDevice(Id) };
            if (await command.TryRunAsync(maxAttempts: 15))
            {
                this.RampRate = rampRate;
                this.IsRampRateRead= true;
                success = true;
            }
        }

        return success;
    }

    /// <summary>
    /// Read channel properties for a KeypadLinc device
    /// </summary>
    /// <param name="channelId">id of the channel to read</param>
    /// <param name="force">whether force a new read from the network even if we already read previously</param>
    /// <returns></returns>
    internal async override Task<bool> TryReadChannelPropertiesAsync(int channelId, bool force = false)
    {
        bool success = true;

        // Read operating flags to ensure we have correct channel count
        // Ignore failure, we will use the operating flags we have currently if this fails
        await TryReadOperatingFlagsAsync();

        if (!(Channels?[channelId - 1]?.ArePropertiesRead ?? false) || force)
        {
            // If this is a device with a single channel, there are no channel properties to read
            // Just do nothing and return success
            if (Channels != null)
            {
                // The device in the model sometimes contains more channels than the physicial device,
                // fail if we get asked to read channels that don't exist
                if (channelId <= ChannelCount)
                {
                    // Now build the channel driver, and read its properties
                    Channels[channelId - 1] ??= new ChannelDriver(this, channelId);
                    success = await Channels[channelId - 1].TryReadChannelProperties();
                }
                else
                {
                    success = false;
                }
            }
        }

        return success;
    }


    /// <summary>
    /// Try to read next unread all-link database record from the physical device and merge it 
    /// in the passed database, setting its status (changed, synced, unknown) for a subsequent write pass.
    /// This method reads nextRecordToRead in AllLinkDatabase.
    /// See base class for details
    /// </summary>
    /// <param name="allLinkDatabase">Database to merge into</param>
    /// <param name="restart">Read the first link in the database if set</param>
    /// <param name="force">Read from start of the database, regardless of the revision and of what was read before</param>
    /// <returns></returns>
    internal override async Task<(bool success, bool done)> TryReadAllLinkDatabaseStepAsync(AllLinkDatabase allLinkDatabase, bool restart, bool force = false)
    {
        bool success = true;

        if (restart)
        {
            int revision = await GetAllLinkDatabaseRevisionAsync();

            // Note that not all devices support database revision numbers (DB Delta).
            // Also, on at least some devices, the revision number is reset when a device loses power,
            // so we can have the revision of the passed database ahead of the revision of the database
            // of the physical device.

            // If 1- the device failed to report a database revision, or
            //    2- the revision of the database on the device is different from the passed dabatase, either
            //       ahead (passed daabase needs to catch up) or behind (the revision was reset on the device).
            //    3- force is set
            // read the whole database
            if (revision == -1 || allLinkDatabase.Revision != revision || force)
                allLinkDatabase.NextRecordToRead = 0;

            if (revision != -1)
                allLinkDatabase.Revision = revision;
        }

        if (!allLinkDatabase.IsRead)
        {
            success = false;

            var command = new GetDeviceLinkRecordCommand(House.Gateway, Id, allLinkDatabase.NextRecordToRead);
            if (await command.TryRunAsync(maxAttempts: 15))
            {
                Debug.Assert(command.AllLinkRecord != null);
                MergeRecord(allLinkDatabase, command.AllLinkRecord, allLinkDatabase.NextRecordToRead);
                if (command.AllLinkRecord.IsHighWatermark)
                    allLinkDatabase.IsRead = true;
                else
                    allLinkDatabase.NextRecordToRead++;
                success = true;
            }
        }

        return (success, allLinkDatabase.IsRead);
    }

    /// <summary>
    /// Try to read yet unread all-link database records from the physical device and merge them in the passed database,
    /// setting their status (changed, synced, unknown) for a subsequent write pass. This method will read records
    /// until the end of the database or an unrecoved error.
    /// This method works from nextRecordToRead in AllLinkDatabase to failure or to end of database.
    /// See MergeRecord for semantics of the merge.
    /// </summary>
    /// <param name="allLinkDatabase">Database to merge into</param>
    /// <param name="force">Read from start of the database, regardless of the revision and of what was read before</param>
    /// <returns>the record sequence number reached before error or end of database</returns>
    internal async override Task<bool> TryReadAllLinkDatabaseAsync(AllLinkDatabase allLinkDatabase, bool force = false)
    {
        // For devices that support database revision numbers (DB Delta), get that revision number
        int revision = await GetAllLinkDatabaseRevisionAsync();


        // See above method for commentary on this
        if (revision == -1 || allLinkDatabase.Revision != revision || force)
            allLinkDatabase.NextRecordToRead = 0;

        if (revision != -1)
            allLinkDatabase.Revision = revision;

        if (!allLinkDatabase.IsRead)
        {
            while (true)
            {
                var command = new GetDeviceLinkRecordCommand(House.Gateway, Id, allLinkDatabase.NextRecordToRead){ MockPhysicalDevice = MockPhysicalDevice };
                if (await command.TryRunAsync(maxAttempts: 15))
                {
                    Debug.Assert(command.AllLinkRecord != null);
                    MergeRecord(allLinkDatabase, command.AllLinkRecord, allLinkDatabase.NextRecordToRead);

                    if (command.AllLinkRecord.IsHighWatermark)
                    {
                        allLinkDatabase.IsRead = true;
                        break;
                    }
                    else
                        allLinkDatabase.NextRecordToRead++;
                }
                else
                {
                    break;
                }
            }
        }

        return allLinkDatabase.IsRead;
    }

    // Helper: try to write one record to the device database
    // <param name="allLinkDatabase">database containing the record to write</param>
    // <param name="seq">Sequence number to write at (0 - current size of database, max: 511)</param>
    // <returns>true if success</returns>
    private async Task<bool> TryWriteAllLinkRecord(AllLinkDatabase allLinkDatabase, int seq)
    {
        bool success = false;

        if (allLinkDatabase.IsRead)
        {
            if (seq < 0 || seq > allLinkDatabase.Count)
            {
                throw new ArgumentException("Attempting to write All-Link record beyond end of database");
            }

            var command = new SetDeviceLinkRecordCommand(House.Gateway, Id, seq, allLinkDatabase[seq]) { MockPhysicalDevice = MockPhysicalDevice };
            if (await command.TryRunAsync(maxAttempts: 15))
            {
                allLinkDatabase.UpdateRecordSyncStatus(seq, SyncStatus.Synced);
                success = true;
            }
        }
        else
        {
            Debug.Assert(false, "Database should have been successfullly read before writing");
        }

        return success;
    }

    /// <summary>
    /// Write not yet synced records in the All-link database to the physical device.
    /// This method should be able to be called repeatedly and only write what needs to be written each time.
    /// For example, writing to the database could be interrupted by the user closing the app and should resume
    /// when the app is restarted.
    /// </summary>
    /// <param name="forceRead">Force reading the database from the physical device prior to writing to it</param>
    /// <param name="allLinkDatabase">the database to write</param>
    /// <returns>true if success. On failure, part of the database might have been rewritten already</returns>
    internal async override Task<bool> TryWriteAllLinkDatabaseAsync(AllLinkDatabase allLinkDatabase, bool forceRead = false)
    {
        bool success;

        // First make sure we have merged in the up-to-date ALL-link database from the network
        success = await TryReadAllLinkDatabaseAsync(allLinkDatabase, forceRead);

        // All devices should have a database with at least one record (the high water mark record)
        if (success && allLinkDatabase != null && allLinkDatabase.Count > 0)
        {
            for (int seq = 0; seq < allLinkDatabase.Count; seq++)
            {
                AllLinkRecord record = allLinkDatabase[seq];

                if (record.SyncStatus != SyncStatus.Synced)
                {
                    // New or changed record, write it to the physical device
                    success = await TryWriteAllLinkRecord(allLinkDatabase, seq);
                    if (!success)
                    {
                        break;
                    }

                    if (record.IsHighWatermark)
                    {
                        // This was the high water mark record, we are done
                        break;
                    }
                }
            }

            // The writes above have incremented the version (delta) number in the physical device.
            // If success, bring database version up to same value.
            if (success)
            {
                int revision = await GetAllLinkDatabaseRevisionAsync();
                if (revision != -1)
                    allLinkDatabase.Revision = revision;
            }
        }

        return success;
    }

    /// <summary>
    /// Merge link record at given index in the physical device database into the corresponding logical device database. 
    /// If the link record at the given seq in the logical database:
    /// - is marked synced, copy the physical record at same index, if different,
    /// - has been deleted (marked changed and not in use), copy the physical record at same index, if different,
    /// - is marked changed, keep it (it will be written to the physical database in a subsequent write operation),
    /// - does not exist (is beyond the end of the logical database), add a copy of the physical record at same index at the end of this database.
    /// All links in the logical database with an index beyond the end of the physical database are kept and marked changed 
    /// as they will need to be written to the physical device in the next write database operation.
    /// </summary>
    /// <param name="logicalDatabase">logical database to merge into</param>
    /// <param name="physicalRecord">physical record to merge in</param>
    /// <param name="seq">index of the physical record to merge in</param>
    private void MergeRecord(AllLinkDatabase logicalDatabase, AllLinkRecord physicalRecord, int seq)
    {
        Debug.Assert(physicalRecord != null);
        if (physicalRecord == null) return;

        if (!physicalRecord.IsHighWatermark)
        {
            if (seq < logicalDatabase.Count && !logicalDatabase[seq].IsHighWatermark)
            {
                Debug.Assert(!logicalDatabase[seq].IsHighWatermark);

                if (logicalDatabase[seq].SyncStatus != SyncStatus.Synced)
                {
                    // For the purpose of the merge, we consider "Unknown" status as "Changed"
                    logicalDatabase.UpdateRecordSyncStatus(seq, SyncStatus.Changed);

                    if (logicalDatabase[seq].IsInUse)
                    {
                        // If record in the logical database is marked "Changed" and in use, keep it
                        if (logicalDatabase[seq].Equals(physicalRecord))
                        {
                            // And mark it "Synced" if it matches the physical record
                            logicalDatabase.UpdateRecordSyncStatus(seq, SyncStatus.Synced);
                        }
                    }
                    else
                    { 
                        // If the record in the logical database was deleted (not in use, "Changed"), propagate the physical record
                        // if different (ignoring in-use), and leave it as is if not different, marking it "synced" if the 
                        // physcial record is already marked not in use.
                        if (!logicalDatabase[seq].Equals(physicalRecord, ignoreInUse: true))
                        {
                            // (We initialize SyncStatus before setting the record so that the change notification contains the proper state)
                            logicalDatabase[seq] = new(physicalRecord) { SyncStatus = SyncStatus.Synced };
                        }
                        else if (!physicalRecord.IsInUse)
                        {
                            logicalDatabase.UpdateRecordSyncStatus(seq, SyncStatus.Synced);
                        }
                    }
                }
                else
                {
                    // If the record in the logical database is marked "Synced", propagate the physical record if it is different
                    // (taking in-use into account) and uses a valid (non-stale) destination device, which might have the effect of
                    // deleting or undeleting the record in the logical database. We let the user deal with that.
                    // If the device is stale, mark the logical record as "Changed" to write the logical record to to device,
                    // hopfully fixing removing the reference to the stale device.
                    // If records are not different, we keep the record from the logical database to avoid changing fields not
                    // stored in the device such as sceneId and we update the sync status to "Synced".
                    if (!logicalDatabase[seq].Equals(physicalRecord))
                    {
                        // In unit-tests where we do not have a devices list, assume the device is valid for the sake of testing
                        if (House.Devices == null || House.GetDeviceByID(physicalRecord.DestID) != null)
                        {
                            logicalDatabase[seq] = new(physicalRecord) { SyncStatus = SyncStatus.Synced };
                        }
                        else
                        {
                            logicalDatabase.UpdateRecordSyncStatus(seq, SyncStatus.Changed);
                        }
                    }
                    else if (logicalDatabase[seq].SyncStatus != SyncStatus.Synced)
                    {
                        logicalDatabase.UpdateRecordSyncStatus(seq, SyncStatus.Synced);
                    }
                }
            }
            else // i >= logicalDatabase.Count || logicalDatabase[i].IsHighWatermark
            {
                // The given physical record is past the end of the logical database, append it to the end if in use
                if (seq < logicalDatabase.Count)
                {
                    logicalDatabase[seq] = new(physicalRecord) { SyncStatus = SyncStatus.Synced };
                }
                else
                {
                    logicalDatabase.Add(new(physicalRecord){ SyncStatus = SyncStatus.Synced });
                }

                logicalDatabase.Add(AllLinkRecord.CreateHighWaterMark(syncStatus: SyncStatus.Synced));
            }
        }
        else  // physicalRecord.IsHighWatermark
        {
            // The last record of the physical database was passed-in
            // If we have more records marked "Synced" in the logical database, remove them from the merged database
            // If we have more not in use record, remove them from the merged database
            // If we have more in use records marked "Unknown", they have probably been added to the logical database, so mark them all "Changed"
            // If we reach the high water mark in the logical database, remove any record past it.
            bool highWaterMarkFound = false;
            for (; seq < logicalDatabase.Count; seq++)
            {
                if (logicalDatabase[seq].IsHighWatermark)
                {
                    highWaterMarkFound = true;
                    continue;
                }

                if (highWaterMarkFound || !logicalDatabase[seq].IsInUse || logicalDatabase[seq].SyncStatus == SyncStatus.Synced)
                {
                    logicalDatabase.RemoveAt(seq);
                    seq--;
                } 
                else if (logicalDatabase[seq].SyncStatus == SyncStatus.Unknown)
                {
                    logicalDatabase.UpdateRecordSyncStatus(seq, SyncStatus.Changed);
                }
            }

            // If last record in logical database was not a high water mark, add it
            if (seq > 0 && !logicalDatabase[seq-1].IsHighWatermark)
            {
                logicalDatabase.Add(AllLinkRecord.CreateHighWaterMark(syncStatus: SyncStatus.Changed));
            }
        }
    }

    /// <summary>
    /// Try to get revision of the all-link database (db delta) from the physical device
    /// This number gets incremented whenever a write operation is made to the all-link database
    /// but it appears to be reset to 0 in some cases, e.g., when there is a loss of power to the device
    /// </summary>
    /// <returns>revision number, -1 if command failed</returns>
    internal async Task<int> GetAllLinkDatabaseRevisionAsync()
    {
        int revision = 0;

        var command = new GetDBDeltaCommand(House. Gateway, Id) { MockPhysicalDevice = MockPhysicalDevice };
        if (await command.TryRunAsync(maxAttempts: 15))
        {
            revision = command.DBDelta;
        }
        else
        {
            revision = -1;
        }

        return revision;
    }

    /// <summary>
    /// Send an cmd via AllLink to all responders in the specificed group (a.k.a., channel)
    /// Command can be any of the direct commands (e.g., LightOn, LightOff, etc.)
    /// Currently not implemented by any device but the Hub.
    /// There might be a way to make this work on keypads with the Trigger All-Link command.
    /// </summary>
    /// <param name="group"></param>
    /// <param name="command"></param>
    /// <param name="level"></param>
    /// <returns></returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    internal override async Task<bool> TrySendAllLinkCommandToGroup(int group, byte command, double level)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        throw new NotImplementedException("SendAllLinkCommandToGroup implemented only by the hub");
    }
}
