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
using Insteon.Commands;
using Insteon.Mock;
using Insteon.Model;
using System.Diagnostics;
using static Insteon.Model.AllLinkRecord;

namespace Insteon.Drivers;

/// <summary>
/// Driver of the physical IM or Hub on the network
/// It caches IM specific state and provides facilities to read and write the IM state
/// </summary>
internal sealed class IMDriver : DeviceDriverBase
{
    internal IMDriver(House house, InsteonID Id) : base(house, Id)
    {
    }

    internal IMDriver(Device device) : base(device)
    {
    }

    /// <summary>
    /// Cache of ALL-Link database in device
    /// </summary>
    private OrderedNonUniqueKeyedList<AllLinkRecord>? physicalAllLinkDatabase { get; set; }

    /// <summary>
    /// For unit-testing purposes
    /// Helper to access the associated mock device as a MockPhysicalIM
    /// </summary>
    internal MockPhysicalIM? MockPhysicalIM => House.GetMockPhysicalDevice(Id) as MockPhysicalIM;

    /// <summary>
    /// Number of channels
    /// TODO: certain IMs models might have a different number of channels
    /// </summary>
    internal override int ChannelCount => 255;

    /// <summary>
    /// Id of the first channel
    /// </summary>
    internal override int FirstChannelId => 0;

    /// <summary>
    /// Get channel default name
    /// </summary>
    /// <param name="channelId"></param>
    /// <returns></returns>
    internal override string GetChannelDefaultName(int channelId) => string.Empty;

    /// <summary>
    /// Ping this device to check that device is connected to the hub
    /// IM is assumed to always be pingable
    /// </summary>
    /// <returns></returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    internal async override Task<Device.ConnectionStatus> TryPingAsync(int maxAttempts = 1)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        return Device.ConnectionStatus.Connected;
    }

    /// <summary>
    /// Ping this device to check that device is connected to the hub
    /// IM is assumed to always be pingable
    /// </summary>
    /// <returns></returns>
#pragma warning disable CS1998
    internal async override Task<bool> TryCleanUpAfterSyncAsync()
#pragma warning restore CS1998
    {
        return true;
    }

    /// <summary>
    /// Check Insteon Engine version
    /// Always 2 for the IM
    /// </summary>
    /// <returns>always true</returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    internal override async Task<bool> TryCheckInsteonEngineVersionAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        return true;
    }

    /// <summary>
    /// Turn light device on at specified level - not implemented for the IM
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
#pragma warning disable 1998
    internal override async Task<bool> TryLightOn(double level)
#pragma warning restore 1998
    {
        return false;
    }

    /// <summary>
    /// Turn light device off at specified level - not implemented for the IM
    /// </summary>
    /// <returns></returns>
#pragma warning disable 1998
    internal override async Task<bool> TryLightOff()
#pragma warning restore 1998
    {
        return false;
    }

    /// <summary>
    /// Return the current on-level of the device load
    /// </summary>
    /// <returns></returns>
#pragma warning disable 1998
    internal override async Task<double> TryGetLightOnLevel()
#pragma warning restore 1998
    {
        return 0;
    }

    /// <summary>
    /// IM does not have operating flags
    /// </summary>
    internal override bool HasOperatingFlags => false;

    /// <summary>
    /// Read device operating flags - not implemented for the IM
    /// </summary>
    /// <returns></returns>
#pragma warning disable 1998
    internal async override Task<bool> TryReadOperatingFlagsAsync(bool force = false)
#pragma warning restore 1998
    {
        return false;
    }

    /// <summary>
    /// Read channel properties if any - not implemented for the IM
    /// </summary>
    /// <returns>success</returns>
#pragma warning disable 1998
    internal async override Task<bool> TryReadChannelPropertiesAsync(int channelId, bool force = false)
#pragma warning restore 1998
    {
        return false;
    }

    /// <summary>
    /// IM does not have LED Brightness and other properties
    /// </summary>
    internal override bool HasOtherProperties => false;

    /// <summary>
    /// Properties that do not exist on the IM
    /// </summary>
    /// <returns>success</returns>
#pragma warning disable 1998
    internal async override Task<bool> TryReadLEDBrightnessAsync() { return false; }
    internal async override Task<bool> TryReadOnLevelAsync() { return false; }
    internal async override Task<bool> TryReadRampRateAsync() { return false; }
#pragma warning restore 1998

    /// <summary>
    /// Properties that do not exist on the IM
    /// </summary>
    /// <returns>true on success</returns>
#pragma warning disable 1998
    internal async override Task<bool> TryWriteLEDBrightnessAsync(byte brightness) { return false; }
    internal async override Task<bool> TryWriteOnLevelAsync(byte onLevel) { return false; }
    internal async override Task<bool> TryWriteRampRateAsync(byte rampRate) { return false; }
#pragma warning restore 1998

    /// <summary>
    /// Try read yet unread all-link database records from the physical IM (hub) and merge them in the passed-in database,
    /// setting their status (changed, synced, unknown) for a subsequent write pass. The IM does not support 
    /// reading from a given record index, so we read the whole database, unless we already read it and know
    /// it has not changed from under us.
    ///
    /// This function merges the database read from the physical device with the passed-in one. It does not modify the physical 
    /// device database (TryWriteAllLinkDatabase will do that), only the passed-in database.
    ///
    /// Merge algorithm: for each ALL-Link record read from the physical device:
    /// - If there is a matching record in the passed-in database, it is retained and marked sync
    /// - If there is no matching record but a similar record exits (same id, group and type)
    ///   > If marked "Changed", the similar record is left as is, so it can be written to the physical database later
    ///     in TryWriteAllLinkDatabase, or deleted from the physical database if it is not in-use
    ///   > If not marked "Changed", the similar record data is updated to mirror the physical database record and marked synced
    /// - If there is no match in the passed-in database, a copy of the physical record is added as "Synced"
    /// All remaining (not matched) link records in the passed-in database are retained and marked "Changed" and they will be 
    /// added to the physical database later in TryWriteAllLinkDatabse, unless they are also marked not in-use, 
    /// in which case they are removed from the passed-in database.
    /// </summary>
    /// <param name="allLinkDatabase">Database to update with the data read from the IM</param>
    /// <param name="force">Read the whole database, even if it was read before</param>
    /// <returns>success</returns>
    internal async override Task<bool> TryReadAllLinkDatabaseAsync(AllLinkDatabase allLinkDatabase, bool force = false)
    {
        bool success = true;

        if (!allLinkDatabase.IsRead || force)
        {
            var command = new GetIMDatabaseCommand(House.Gateway) { MockPhysicalIM = MockPhysicalIM };
            if (await command.TryRunAsync())
            {
                physicalAllLinkDatabase = command.AllLinkDatabase;
            }
            else
            {
                success = false;
            }

            if (success)
            {
                MergeDatabase(allLinkDatabase, physicalAllLinkDatabase);
                allLinkDatabase.IsRead = true;
            }
        }

        return success;
    }

    /// <summary>
    /// Merge the passed-in logcial database with the physical device database, 
    /// setting the status of each record, and adding missing records on both sides as needed
    /// </summary>
    /// <param name="allLinkDatabase">logical database</param>
    /// <param name="physicalAllLinkDatabase">device physical database</param>
    /// <returns></returns>
    private static void MergeDatabase(AllLinkDatabase allLinkDatabase, OrderedNonUniqueKeyedList<AllLinkRecord>? physicalAllLinkDatabase)
    {
        // Keep track of records matched in the logical database
        HashSet<AllLinkRecord> matchedRecordSet = new HashSet<AllLinkRecord>(ReferenceEqualityComparer.Instance);

        // Keep track of record we will have to remove from the physical database
        List<AllLinkRecord> recordsToRemove = new List<AllLinkRecord>();

        // Iterate through the database of the physical device
        Debug.Assert(physicalAllLinkDatabase != null);
        foreach (AllLinkRecord record in physicalAllLinkDatabase)
        {
            bool wasRecordMatched = false;

            // These should not exist in the device database, but just in case...
            if (!record.IsInUse || record.IsHighWatermark)
            {
                continue;
            }

            // Get records in the logical database matching destination device Id of the physical record
            if (allLinkDatabase.TryGetMatchingEntries(record.GetHashCode(), out List<AllLinkRecord>? idMatchingRecords))
            {
                // Look for a yet unmatched, fully matching record, and if found, mark it with a "Synced" status
                // since we have a match in the physical database. Add it to the list of matched records.
                foreach (var matchingCandidateRecord in idMatchingRecords)
                {
                    if (matchingCandidateRecord.Equals(record) && !matchedRecordSet.Contains(matchingCandidateRecord))
                    {
                        wasRecordMatched = true;
                        var seq = allLinkDatabase.TryGetRecordSeqByUid(matchingCandidateRecord.Uid);
                        matchedRecordSet.Add(allLinkDatabase.UpdateRecordSyncStatus(seq, SyncStatus.Synced));
                        break;
                    }
                }

                // If no fully matching record was found, look for a yet unmatched, similar record (matching id, group, type)
                // and add it to the matched records
                // If that record is not in use, leave it as is and it will be removed from the physical database in TryWriteAllLinkDatabase.
                // If in use, and marked "Changed" or "Unknown" leave it as Changed and it will be updated in the physical database.
                // If in use, and marked "Synced" update its data from the physical database.
                if (!wasRecordMatched)
                {
                    foreach (var matchingCandidateRecord in idMatchingRecords)
                    {
                        if (matchingCandidateRecord.Group == record.Group &&
                            matchingCandidateRecord.IsController == record.IsController &&
                            !matchedRecordSet.Contains(matchingCandidateRecord))
                        {
                            AllLinkRecord matchedRecord = matchingCandidateRecord;

                            if (matchingCandidateRecord.IsInUse && matchingCandidateRecord.SyncStatus == SyncStatus.Synced)
                            {
                                matchedRecord = new(record) { SyncStatus = SyncStatus.Synced };
                                allLinkDatabase.ReplaceRecord(matchingCandidateRecord, matchedRecord);
                            }
                            else if (matchedRecord.SyncStatus == SyncStatus.Unknown)
                            {
                                var seq = allLinkDatabase.TryGetRecordSeqByUid(matchedRecord.Uid);
                                matchedRecord = allLinkDatabase.UpdateRecordSyncStatus(seq, SyncStatus.Changed);
                            }

                            wasRecordMatched = true;
                            matchedRecordSet.Add(matchedRecord);
                            break;
                        }
                    }
                }
            }

            if (!wasRecordMatched)
            {
                // If the physical record was not matched in the logical database, add a copy of it with "Synced" status to the logical database
                AllLinkRecord logicalRecord = new(record) { SyncStatus = SyncStatus.Synced };
                allLinkDatabase.Add(logicalRecord);
                matchedRecordSet.Add(logicalRecord);
            }
        }

        // Now mark in-use records of the logical database we did not match above as "Changed".
        // They have probably been deleted in the hub from under us, so we re-add them and
        // let the user sort it out, e.g., by deleting them explicitly.
        for (int seq = 0; seq < allLinkDatabase.Count; seq++)
        {
            var record = allLinkDatabase[seq];
            if (!matchedRecordSet.Contains(record) && record.IsInUse && !record.IsHighWatermark)
            {
                allLinkDatabase.UpdateRecordSyncStatus(seq, SyncStatus.Changed);
            }
        }
    }

    /// <summary>
    /// Try read next unread record. Not supported by the IM. Read the whole database instead.
    /// See base class for details.
    /// </summary>
    /// <param name="allLinkDatabase">Database to update with the data read from the IM</param>
    /// <param name="restart">This is the first step</param>
    /// <param name="force">Read the whole database, even if it was read before</param>
    /// <returns></returns>
    internal override async Task<(bool success, bool done)> TryReadAllLinkDatabaseStepAsync(AllLinkDatabase allLinkDatabase, bool restart, bool force = false)
    {
        bool success = true;
        bool done = false;

        if (!allLinkDatabase.IsRead || force)
        {
            if (restart)
            {
                physicalAllLinkDatabase = new AllLinkDatabase();
                var cmd = new GetIMFirstAllLinkRecordCommand(House.Gateway) { SuppressLogging = true };
                if (await cmd.TryRunAsync())
                {
                    Debug.Assert(cmd.Record != null);
                    physicalAllLinkDatabase.Add(cmd.Record);
                }
                else if (cmd.ErrorReason == Command.ErrorReasons.NAK)
                {
                    done = true;
                }
            }
            else
            {
                var cmd = new GetIMNextAllLinkRecordCommand(House.Gateway) { SuppressLogging = true };
                if (await cmd.TryRunAsync(maxAttempts: 10))
                {
                    Debug.Assert(cmd.Record != null);
                    physicalAllLinkDatabase?.Add(cmd.Record);
                }
                else if (cmd.ErrorReason == Command.ErrorReasons.NAK)
                {
                    done = true;
                }
            }

            if (success && done)
            {
                MergeDatabase(allLinkDatabase, physicalAllLinkDatabase);
                allLinkDatabase.IsRead = true;
            }
        }
        else
        {
            done = true;
        }

        return (success, done);
    }

    /// <summary>
    /// Try to write Operating Flags to the device  - no-op for the IM
    /// TODO: Could be used later for IM config
    /// </summary>
    /// <param name="operatingFlags">Operating flags to write</param>
    /// <param name="opFlags2">Operating flags to write, second byte</param>
    /// <returns>success</returns>
#pragma warning disable 1998
    internal async override Task<bool> TryWriteOperatingFlagsAsync(Bits operatingFlags, Bits opFlags2)
#pragma warning restore 1998
    {
        return true;
    }

    /// <summary>
    /// Write not yet synced All-Link Database records to the physical device
    /// See base class for details
    /// </summary>
    /// <param name="forceRead">Force reading the database from the physical device prior to writing to it</param>
    /// <param name="allLinkDatabase">the new database to write</param>
    /// <returns>success</returns>
    internal async override Task<bool> TryWriteAllLinkDatabaseAsync(AllLinkDatabase allLinkDatabase, bool forceRead)
    {
        // Don't allow to wipe the entire database with a null argument
        if (allLinkDatabase == null)
        {
            return false;
        }

        // The device database needs to have been read and sync status of the records updated
        bool success = await TryReadAllLinkDatabaseAsync(allLinkDatabase, force: forceRead);

        // Remove duplicate records in the database to write
        allLinkDatabase.RemoveDuplicateRecords();

        if (success)
        {
            var recordsToRemove = new List<AllLinkRecord>();

            for (int seq = 0; seq < allLinkDatabase.Count; seq++)
            {
                var record = allLinkDatabase[seq];
                if (record.SyncStatus == SyncStatus.Changed)
                {
                    if (record.IsInUse)
                    {
                        bool editOrAddRecord = true;

                        // First find all records matching id, group, type (controller/responder) with "record" in the physical device
                        if (physicalAllLinkDatabase?.TryGetMatchingEntries(record, IdGroupTypeComparer.Instance, out List<AllLinkRecord>? matchingRecords) ?? false)
                        {
                            Debug.Assert(matchingRecords.Count > 0);
                            if (matchingRecords.Count == 1)
                            {
                                // If there is exactly one matching record in the physical device, 
                                // only write this record to the physical device if it is different (i.e., data1/2/3 is different)
                                editOrAddRecord = !matchingRecords[0].Equals(record);
                            }
                            else
                            {
                                // If there are multiple matching records in the physical device
                                // delete them all and we will add back the current record of the passed-in database record
                                success = await TryDeleteMatchingRecordsOfSameDir(record);
                            }
                        }

                        // If current record of the passed-in database is not in the physical device, modify matching record or add it
                        if (success && editOrAddRecord)
                        {
                            success = await TryEditOrAddRecord(record);
                        }

                        // Keep AllLinkDatabase of this physical device in sync with modifications made to the actual physical device
                        if (success)
                        {
                            allLinkDatabase.UpdateRecordSyncStatus(seq, SyncStatus.Synced);
                            record.LogDebug(seq, (editOrAddRecord ? "Written to" : "Matched in") + " physical device:");
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        // Delete this record from the ALL-Link database of the physical device
                        DeleteRecordReturnCodes returnCode = await TryDeleteFirstMatchingRecord(record);

                        // If successfully removed or not found, remove the record from the passed-in database
                        if (returnCode == DeleteRecordReturnCodes.Success)
                        {
                            recordsToRemove.Add(record);
                            record.LogDebug(-1, "Removed from physical device:");
                        }
                        else if (returnCode == DeleteRecordReturnCodes.NotFound)
                        {
                            recordsToRemove.Add(record);
                            record.LogDebug(-1, "Not Found in physical device:");
                        }
                        else
                        {
                            success = false;
                            break;
                        }
                    }
                }
                else
                {
                    record.LogDebug(seq, "Already synced with physical device:");
                }
            }

            // Now actually remove the records from the passed-in database
            foreach(var record in recordsToRemove)
            {
                allLinkDatabase.Remove(record);
            }
        }

        return success;
    }

    /// <summary>
    /// Edit or add specified record
    /// If record matches id, group, and type (Controller/Responder) in the database, edit that record
    /// Otherwise add new record
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private async Task<bool> TryEditOrAddRecord(AllLinkRecord record)
    {
        bool success;

        // Add or edit record that changed
        if (record.IsController)
        {
            var cmd = new EditIMFirstFoundOrAddControllerAllLinkRecordCommand(House.Gateway, record) { MockPhysicalIM = MockPhysicalIM };
            success = await cmd.TryRunAsync();
        }
        else
        {
            var cmd = new EditIMFirstFoundOrAddResponderAllLinkRecordCommand(House.Gateway, record) { MockPhysicalIM = MockPhysicalIM };
            success = await cmd.TryRunAsync();
        }

        // The controller/responder specific commands above appear to fail with a NAK sometimes
        // Try with the more generic command in that case
        if (!success)
        {
            var cmd = new EditIMFirstFoundOrAddAllLinkRecordCommand(House.Gateway, record) { MockPhysicalIM = MockPhysicalIM };
            success = await cmd.TryRunAsync();
        }

        // Update cache of physical database if we have one
        if (success && physicalAllLinkDatabase != null)
        {
            if (physicalAllLinkDatabase.TryGetEntry(record, IdGroupTypeComparer.Instance, out AllLinkRecord? matchingRecord))
            {
                if (record != matchingRecord)
                {
                    var index = physicalAllLinkDatabase.IndexOf(matchingRecord);
                    physicalAllLinkDatabase[index] = new AllLinkRecord(record);
                }
            }
            else
            {
                physicalAllLinkDatabase.Add(record);
            }
        }

        return success;
    }

    /// <summary>
    /// Delete the specified record in the physical device
    /// </summary>
    /// <param name="record"></param>
    /// <returns>success</returns>
    private async Task<bool> TryDeleteRecord(AllLinkRecord record)
    {
        // First find all records matching id and group with "record" in the device database
        List<AllLinkRecord> matchingRecords = await FindMatchingRecords(record);
        bool success = matchingRecords != null;

        // Then delete all the matches in the physical device (if any)
        if (success && matchingRecords != null && matchingRecords.Count > 0)
        {
            success = await TryDeleteMatchingRecords(record);
        }

        // Then re-write the ones we did not mean to delete, if any
        if (success && matchingRecords != null && matchingRecords.Count > 1)
        {
            foreach (AllLinkRecord deletedRecord in matchingRecords)
            {
                if (!deletedRecord.Equals(record))
                {
                    var cmd = new EditIMFirstFoundOrAddAllLinkRecordCommand(House.Gateway, deletedRecord) { MockPhysicalIM = MockPhysicalIM };
                    if (await cmd.TryRunAsync())
                    {
                        // Add record to the local cache of the physical device database, if we have one
                        if (physicalAllLinkDatabase != null)
                        {
                            physicalAllLinkDatabase.Add(deletedRecord);
                        }
                    }
                    else
                    {
                        success = false;
                    }
                }
            }
        }

        return success;
    }

    /// <summary>
    /// Delete all records matching id, group and type (controller/responder) of the specificed record in the physical device
    /// </summary>
    /// <param name="record"></param>
    /// <returns>success</returns>
    private async Task<bool> TryDeleteMatchingRecordsOfSameDir(AllLinkRecord record)
    {
        // First find all records matching id and group with "record" in the device database
        List<AllLinkRecord> matchingRecords = await FindMatchingRecords(record);
        bool success = matchingRecords != null;

        // Then delete all the matches in the physical device (if any)
        if (success && matchingRecords != null && matchingRecords.Count > 0)
        {
            success = await TryDeleteMatchingRecords(record);
        }

        // Then re-write the ones that did not match type
        if (success && matchingRecords != null && matchingRecords.Count > 1)
        {
            foreach (AllLinkRecord deletedRecord in matchingRecords)
            {
                if (deletedRecord.IsController != record.IsController)
                {
                    var cmd = new EditIMFirstFoundOrAddAllLinkRecordCommand(House.Gateway, deletedRecord) { MockPhysicalIM = MockPhysicalIM };
                    if (await cmd.TryRunAsync())
                    {
                        // Add record to the local cache of the physical device database, if we have one
                        if (physicalAllLinkDatabase != null)
                        {
                            physicalAllLinkDatabase.Add(deletedRecord);
                        }
                    }
                    else
                    {
                        success = false;
                    }
                }
            }
        }

        return success;
    }

    // Returns the list of record matching a given record in the device database
    private async Task<List<AllLinkRecord>> FindMatchingRecords(AllLinkRecord record)
    {
        bool success = false;
        List<AllLinkRecord> matchingRecords = new List<AllLinkRecord>();
        {
            var cmd = new FindIMFirstAllLinkRecordCommand(House.Gateway, record);
            if (await cmd.TryRunAsync())
            {
                Debug.Assert(cmd.Record != null);
                success = true;
                matchingRecords.Add(cmd.Record);
            }
        }

        if (success)
        {
            var cmd = new FindIMNextAllLinkRecordCommand(House.Gateway, record) { MockPhysicalIM = MockPhysicalIM };
            while (await cmd.TryRunAsync())
            {
                Debug.Assert(cmd.Record != null);
                matchingRecords.Add(cmd.Record);
            }
        }
        return matchingRecords;
    }

    /// <summary>
    /// Delete all records matching id and group of the specificed record in the physical device
    /// </summary>
    /// <param name="record"></param>
    /// <returns>success</returns>
    private async Task<bool> TryDeleteMatchingRecords(AllLinkRecord record)
    {
        bool success = false;

        do
        {
            if (await TryDeleteFirstMatchingRecord(record) == DeleteRecordReturnCodes.Success)
            {
                // Found at least one record, that's success!
                success = true;
            }
            else
            {
                break;
            }
        } while (true);

        return success;
    }

    /// <summary>
    /// Delete first record matching id and group of the specificed record in the physical device
    /// </summary>
    /// <param name="record"></param>
    /// <returns>DeleteRecordReturnCodes</returns>
    private async Task<DeleteRecordReturnCodes> TryDeleteFirstMatchingRecord(AllLinkRecord record)
    {
        DeleteRecordReturnCodes returnCode = DeleteRecordReturnCodes.Success;

        // The IsInUse flag is set in the physical device and database cache
        // Set it here to ensure a match
        record = new(record) { IsInUse = true };

        var cmd = new DeleteIMFirstFoundAllLinkRecordCommand(House.Gateway, record) { MockPhysicalIM = MockPhysicalIM };
        if (await cmd.TryRunAsync())
        {
            // Delete from the local cache of the physical device database, if we have one
            if (physicalAllLinkDatabase != null)
            {
                physicalAllLinkDatabase.RemoveMatchingEntry(record);
            }
        }
        else
        {
            returnCode = cmd.ErrorReason == Command.ErrorReasons.NAK ? DeleteRecordReturnCodes.NotFound : DeleteRecordReturnCodes.Other;
        }

        return returnCode;
    }

    /// <summary>
    /// Return codes for delete record method(s)
    /// </summary>
    internal enum DeleteRecordReturnCodes
    {
        Success = 0,
        NotFound,
        Other
    }

    /// <summary>
    /// Returns a list of records in the physical device that match a given record
    /// Match is on Id and Group, and type (controller/responder) if matchLinkDir is true
    /// </summary>
    /// <param name="record">record to match on</param>
    /// <param name="matchLinkDir">whether to match on (controller/responder) in addition to id and group</param>
    /// <returns>List of matching records</returns>
    private async Task<List<AllLinkRecord>?> TryReadMatchingRecords(AllLinkRecord record, bool matchLinkDir)
    {
        List<AllLinkRecord>? returnedRecords = null;
        bool recordFound = false;

        {
            var cmd = new FindIMFirstAllLinkRecordCommand(House.Gateway, record) { MockPhysicalIM = MockPhysicalIM };
            if (await cmd.TryRunAsync())
            {
                Debug.Assert(cmd.Record != null);
                recordFound = true;
                if (!matchLinkDir || record.IsController == cmd.Record.IsController)
                {
                    returnedRecords = new List<AllLinkRecord>
                    {
                        cmd.Record
                    };
                }
            }
            else if (cmd.ErrorReason == Command.ErrorReasons.NAK)
            {
                recordFound = false;
            }
        }

        while (recordFound)
        {
            var cmd = new FindIMNextAllLinkRecordCommand(House.Gateway, record) { MockPhysicalIM = MockPhysicalIM };
            if (await cmd.TryRunAsync())
            {
                Debug.Assert(cmd.Record != null);
                if (!matchLinkDir || record.IsController == cmd.Record.IsController)
                {
                    if (returnedRecords == null)
                    {
                        returnedRecords = new List<AllLinkRecord>();
                    }
                    returnedRecords.Add(cmd.Record);
                }
            }
            else if (cmd.ErrorReason == Command.ErrorReasons.NAK)
            {
                recordFound = false;
            }
        }

        return returnedRecords;
    }

    /// <summary>
    /// Send an cmd via AllLink to all responders in the specificed group (a.k.a., channel)
    /// Command can be any of the direct commands (e.g., LightOn, LightOff, etc.)
    /// </summary>
    /// <param name="group"></param>
    /// <param name="command"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    internal override async Task<bool> TrySendAllLinkCommandToGroup(int group, byte command, double level)
    {
        int levelInt = (int)(level * 255);
        if (levelInt < 0) levelInt = 0;
        if (levelInt > 255) levelInt = 255;
        var cmd = new SendAllLinkCommand(House.Gateway, (byte)group, command, (byte)levelInt) { MockPhysicalIM = MockPhysicalIM };
        return await cmd.TryRunAsync();
    }
}
