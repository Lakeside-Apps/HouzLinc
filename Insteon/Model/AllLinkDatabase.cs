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

namespace Insteon.Model;

/// <summary>
/// A representation of the ALL-Link Insteon database in a deviced
/// Used for logical devices (in the model) and for physical devices
/// </summary>
public sealed class AllLinkDatabase : OrderedNonUniqueKeyedList<AllLinkRecord>
{
    /// <summary>
    /// Use to crate a database not associated to any particular device
    /// </summary>
    internal AllLinkDatabase() : base()
    {
    }

    /// <summary>
    /// Use to create a database associated to a device
    /// </summary>
    /// <param name="device"></param>
    internal AllLinkDatabase(Device device)
    {
        this.Device = device;
    }

    /// <summary>
    /// Make a deep copy of the database, optionally ommitting links to a specific device
    /// </summary>
    /// <param name="from">copy source</param>
    /// <param name="idToExclude">device to eliminate any link to</param>
    internal AllLinkDatabase(AllLinkDatabase from, InsteonID? idToExclude = null)
    {
        foreach (var record in from)
        {
            if (idToExclude == null || record.DestID != idToExclude)
            {
                Add(new AllLinkRecord(record));
            }
        }

        NextRecordToRead = from.NextRecordToRead;
        ReadResumeData = from.ReadResumeData;
        Sequence = from.Sequence;
        Revision = from.Revision;
        LastStatus = from.LastStatus;
        LastUpdate = from.LastUpdate;
    }

    /// <summary>
    /// Copy state from another AllLinkDatabase.
    /// The database we copy from is assumed to have the "thruth" about the records.
    /// Returns this if the database are identical and the "from" database if not.
    /// The caller should generate an AllLinkDatabaseChanged observer notification 
    /// if the return value is different from this.
    /// </summary>
    /// <param name="fromDatabase"></param>
    /// <returns>the resulting database</returns>
    internal AllLinkDatabase CopyFrom(AllLinkDatabase fromDatabase)
    {
        // TODO: once the rest of the model persistence pipeline is in place,
        // consider optimizing this and copying to this only the records that actually changed.
        // See commented out code below for an example. Will require unit-tests.

        if (IsIdenticalTo(fromDatabase))
        {
            return this;
        }
        return fromDatabase;

        //int seq = 0;
        //foreach (var record in from)
        //{
        //    if (!record.Equals(this[seq]))
        //    {
        //        if (from.TryGetEntry(this[seq], null, out _))
        //        {
        //            this.Insert(seq, record);
        //        }
        //        else
        //        {
        //            this[seq] = record;
        //        }
        //    }
        //    seq++;
        //}

        //LastStatus = from.LastStatus;
        //LastUpdate = from.LastUpdate;
        //NextRecordToRead = from.NextRecordToRead;
        //ReadResumeData = from.ReadResumeData;
        //Sequence = from.Sequence;
        //Revision = from.Revision;
    }

    /// <summary>
    /// Whether this database is strictly identical to another
    /// </summary>
    /// <param name="database"></param>
    /// <returns></returns>
    internal bool IsIdenticalTo(AllLinkDatabase database)
    {
        if (Revision != database.Revision ||
            Count != database.Count ||
            Sequence != database.Sequence ||
            //ReadResumeData != database.ReadResumeData ||
            LastStatus != database.LastStatus)
        {
            return false;
        }

        if (Count != database.Count)
            return false;

        for (var i = 0; i < Count; i++)
        {
            if (!this[i].IsIdenticalTo(database[i]))
            {
                return false;
            }
        }

        return true;
    }

    // Called when the model has been fully deserialized
    internal void OnDeserialized()
    {
        if (House != null)
            AddObserver(House.ModelObserver);

        // Fix any link/database sync status consistency
        foreach (var record in this)
        {
            AdjustLastStatus(record.SyncStatus);
            if (LastStatus == SyncStatus.Changed)
                break;
        }
    }

    // Helper to adjust the database status when a record status changed
    void AdjustLastStatus(SyncStatus syncStatus)
    {
        if (syncStatus != SyncStatus.Synced)
        {
            // Adjust the sync status of the database if needed
            if (syncStatus == SyncStatus.Changed)
            {
                LastStatus = SyncStatus.Changed;
            }
            else if (LastStatus == SyncStatus.Synced)
            {
                LastStatus = SyncStatus.Unknown;
            }
        }
    }

    // Access the house if this database is associated with a device
    public House? House => Device?.House;

    /// <summary>
    /// Observers can subscribe/unsubscribe to notifications of changes in the database
    /// </summary>
    /// <param name="observer"></param>
    /// <returns>this so that it can be chained behind new()</returns>
    public AllLinkDatabase AddObserver(IAllLinkDatabaseObserver observer)
    {
        observers.Add(observer);
        return this;
    }
    public AllLinkDatabase RemoveObserver(IAllLinkDatabaseObserver observer)
    {
        observers.Remove(observer);
        return this;
    }
    private List<IAllLinkDatabaseObserver> observers = new List<IAllLinkDatabaseObserver>();

    // Overrides of the base class notifications that the list is changing
    protected override void ClearItems()
    {
        base.ClearItems();
        observers.ForEach(o => o.AllLinkDatabaseCleared(Device));
    }

    protected override void InsertItem(int index, AllLinkRecord item)
    {
        Debug.Assert(index == Count, "Only adding a record at the end of an all-link database is supported");
        base.InsertItem(index, item);
        observers.ForEach(o => o.AllLinkRecordAdded(Device, item));
    }

    protected override void RemoveItem(int index)
    {
        var removedItem = this[index];
        base.RemoveItem(index);
        observers.ForEach(o => o.AllLinkRecordRemoved(Device, removedItem));
    }

    protected override void SetItem(int index, AllLinkRecord newItem)
    {
        var itemToReplace = this[index];
        base.SetItem(index, newItem);
        observers.ForEach(o => o.AllLinkRecordReplaced(Device, itemToReplace, newItem));
    }

    // Update record SyncStatus and notify observers
    public AllLinkRecord UpdateRecordSyncStatus(int index, SyncStatus syncStatus)
    {
        // No need to generate a change record if the status is not changing
        if (this[index].SyncStatus == syncStatus)
            return this[index];

        AdjustLastStatus(syncStatus);
        return this[index] = new(this[index]) { SyncStatus = syncStatus };
    }

    /// <summary>
    /// Revision number of the database (obtained by GetDBDelta from Insteon devices)
    /// </summary>
    internal int Revision
    {
        get => revision;
        set
        {
            if (value != revision)
            {
                revision = value;
                observers?.ForEach(o => o.AllLinkDatabasePropertiesChanged(Device));
            }
        }
    }
    private int revision;

    /// <summary>
    /// Last record read if revision matches physical device, -1 if fully read
    /// </summary>
    internal int NextRecordToRead
    {
        get => nextRecordToRead;
        set
        {
            if (value != nextRecordToRead)
            {
                nextRecordToRead = value;
                observers?.ForEach(o => o.AllLinkDatabasePropertiesChanged(Device));
            }
        }
    }
    private int nextRecordToRead;

    /// <summary>
    /// Helper to get or set read status
    /// </summary>
    internal bool IsRead 
    {
        get => NextRecordToRead == -1;
        set => NextRecordToRead = value ? -1 : 0;
    }

    /// <summary>
    /// Legacy, not used: only kep to round trip to houselinc.xml
    /// </summary>
    internal int Sequence;

    // Device holding this All-Link database, if any
    internal Device? Device;

    /// <summary>
    /// Database sync status
    /// Setting this property updates LastUpdate we are observing change (i.e., when not deserializing)
    /// </summary>
    internal SyncStatus LastStatus
    {
        get => lastStatus;
        set
        {
            if (value != lastStatus)
            {
                lastStatus = value;
                LastUpdate = DateTime.Now;
                observers?.ForEach(o => o.AllLinkDatabaseSyncStatusChanged(Device));
            }
        }
    }
    private SyncStatus lastStatus;

    /// <summary>
    /// Last time the status was set to changed or sync
    /// </summary>
    internal DateTime LastUpdate;

    /// <summary>
    /// Not used right now but keeping it for roundtriping to houselinc.xml
    /// </summary>
    public sealed class ReadResumeDataType
    {
        public string? TimeStamp { get; set; }
        public int Sequence { get; set; }
        public int LastSuccessIndex { get; set; }
    }
    internal ReadResumeDataType? ReadResumeData;

    /// <summary>
    /// Try to get a record in this database by its uid
    /// </summary>
    /// <param name="uid"></param>
    /// <returns></returns>
    public AllLinkRecord TryGetRecordByUid(uint uid)
    {
        foreach (var record in this)
        {
            if (record.Uid == uid)
                return record;
        }
        return null!;
    }

    /// <summary>
    /// Try to get a record seq number (index) in this database by its uid
    /// </summary>
    /// <param name="uid"></param>
    /// <returns>sequence number or -1 if record not found</returns>
    public int TryGetRecordSeqByUid(uint uid)
    {
        for (int seq = 0; seq < Count; seq++)
        {
            if (this[seq].Uid == uid)
                return seq;
        }
        return -1;
    }

    /// <summary>
    /// Elementary replace of a record with a given uid by another.
    /// This is a low-level method, like this[..], that just performs the replacement
    /// with no attempt to maintain the semantics of the database (e.g., high water mark).
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="newRecord"></param>
    /// <returns>success</returns>
    public bool Replace(uint uid, AllLinkRecord newRecord)
    {
        var i = TryGetRecordSeqByUid(uid);
        if (i != -1)
        {
            this[i] = newRecord;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Elementary removal of a record with a given uid.
    /// This is a low-level method, like RemoveAt(..), that just performs the removal
    /// with no attempt to maintain the semantics of the database.
    /// </summary>
    /// <param name="uid"></param>
    /// <returns></returns>
    public bool Remove(uint uid)
    {
        var i = TryGetRecordSeqByUid(uid);
        if (i != -1)
        {
            RemoveAt(i);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Add a record in first "not in used" spot or at the end of the database 
    /// if no such spot exist, properly updating the high water mark.
    /// The new record sync status is unchanged and can be set by the caller.
    /// The database status is marked "Changed" if the record is not "Synced".
    /// </summary>
    /// <param name="record"></param>
    /// <return>Success</return>
    public bool AddRecord(AllLinkRecord record)
    {
        // The Add/ReplaceRecord methods automatically maintain the high water mark
        // We ignore any attempt to set the high water mark here.
        if (record.IsHighWatermark)
            return false;

        // Look for a "not in used" record
        int seq = 0;
        for (; seq < Count; seq++)
        {
            if (!this[seq].IsInUse) 
                break;
        }

        if (seq < Count && !this[seq].IsHighWatermark)
        {
            // Found one, use it
            this[seq] = record;
        }
        else
        {
            // Did not find one, add record at the end
            // If a high water mark is present, overwrite high water mark with new record
            // and re-add a new high water mark after it, with the same uid
            uint hwmUid = 0;
            if (seq < Count)
            {
                hwmUid = this[seq].Uid;
                this[seq] = record;
            }
            else
            {
                Add(record);
            }

            seq++;

            if (seq < Count)
            {
                this[seq] = AllLinkRecord.CreateHighWaterMark(uid: hwmUid, record.SyncStatus);
            }
            else
            {
                Add(AllLinkRecord.CreateHighWaterMark(uid: hwmUid, record.SyncStatus));
            }
        }

        if (record.SyncStatus != SyncStatus.Synced)
            LastStatus = SyncStatus.Changed;

        return true;
    }

    /// <summary>
    /// Set a record at a given sequence number in the database, possibly at or beyond current max index.
    /// No effort is made to maintain the high water mark, caller needs to take care of that.
    /// The new record sync status is left unchanged, and can be set by the caller
    /// The database status is marked "Changed" if the record is not "Synced".
    /// </summary>
    /// <param name="seq"></param>
    /// <param name="record"></param>
    internal void SetRecordAt(int seq, AllLinkRecord record)
    {
        if (seq < 0 || seq == Count)
        {
            seq = Count;
            Add(record);
        }
        else if (seq < Count)
        {
            this[seq] = record;
        }
        else
        {
            // If sequence number is greater than the current size of the database, add appropriate number of null records, then add record
            // Otherwise add record at the sequence number, ensuring that slot does not contain a record already
            // This is necessary because we don't always acquire the record from the physical device in sequential order
            for (int i = Count; i < seq; i++)
            {
                Add(null!);
            }
            Add(record);
        }

        if (record.SyncStatus != SyncStatus.Synced)
            LastStatus = SyncStatus.Changed;
    }

    /// <summary>
    /// Swap two records in the database.
    /// This is handled in such a way it can be replayed from recorded changes
    /// that use uids to identify the records.
    /// The two records and the database are markd changed.
    /// </summary>
    /// <param name="seq1">valid seq of first record to swap</param>
    /// <param name="seq2">valid seq of second record to swap</param>
    internal bool SwapRecords(int seq1, int seq2)
    {
        Debug.Assert(seq1 >= 0 && seq1 < Count);
        Debug.Assert(seq2 >= 0 && seq2 < Count);

        // Can't swap with high water mark
        if (this[seq1].IsHighWatermark) return false;
        if (this[seq2].IsHighWatermark) return false;

        // Remember the original records
        AllLinkRecord record1 = this[seq1];
        AllLinkRecord record2 = this[seq2];

        // Swap the records
        // This creates a new (temporary) uid for record at seq1.
        // Necessary to avoid uid duplication which would fail the swap when playing recorded changes.
        this[seq1] = new(record2) { Uid = 0, SyncStatus = SyncStatus.Changed };
        this[seq2] = new(record1) { SyncStatus = SyncStatus.Changed };
        
        // Restore the original uid for record at seq1
        this[seq1] = new(this[seq1]) { Uid = record2.Uid, SyncStatus = SyncStatus.Changed };

        LastStatus = SyncStatus.Changed;

        return true;
    }

    /// <summary>
    /// If this is the hub, remove matching occurence of a record in the database
    /// If this is a regular device, remove the last occurence of a given record from this database 
    /// by marking it not in use
    /// If marked not in use, the record sync status is left unchanged, and can be set by the caller
    /// (but the database itself is marked changed)
    /// </summary>
    /// <param name="record">record to remove</param>
    /// <returns>Success</returns>
    public bool RemoveRecord(AllLinkRecord record)
    {
        return ReplaceRecord(record, new(record){ IsInUse = false });
    }

    /// <summary>
    /// Replace the first occurence matching a given record in the database by another record,
    /// unless the replacement record is not-in-use, in which case we replace the last occurence,
    /// to keep the unused records toward the end of the database as much as posisble.
    /// This will fail if attempting to replace a record that does not exist.
    /// The new record sync status is left unchanged, and can be set by the caller
    /// The database status is marked "Changed" if the record is not "Synced".
    /// </summary>
    /// <param name="recordToReplace">record to remove, has to have same reference as record in the database</param>
    /// <param name="newRecord"></param>
    /// <returns>Success</returns>
    public bool ReplaceRecord(AllLinkRecord recordToReplace, AllLinkRecord newRecord)
    {
        // The Add/ReplaceRecord methods automatically maintain the high water mark
        // We ignore any attempt to set the high water mark here.
        if (newRecord.IsHighWatermark)
            return false;

        // Attempt to replace the high water mark. Consider this an add.
        if (recordToReplace.IsHighWatermark)
            return AddRecord(newRecord);

        // Attempt to replace.
        // If the replacing record is not in use, search from the end of the database
        // to keep the unused records toward the end of the database as much as posisble
        // if we are trying to delete duplicate records.
        int i = IndexOf(recordToReplace, searchInReverse: !newRecord.IsInUse);
        if (i != -1)
        {
            this[i] = newRecord;
            if (newRecord.SyncStatus != SyncStatus.Synced)
                LastStatus = SyncStatus.Changed;
            return true;
        }

        // If we were trying to replace a not-in-use record, but did not find it in the database
        // something took that unused slot from under us. In that case, we simply add the new record
        // (either in the first available slot or at the end of the database).
        if (!recordToReplace.IsInUse)
        {
            return AddRecord(new(newRecord){ SyncStatus = SyncStatus.Changed });
        }

        return false;
    }


    /// <summary>
    /// Remove duplicate records
    /// This does not work for the hub, which given the way its database works, should never have duplicate records
    /// </summary>
    public void RemoveDuplicateRecords()
    {
        if (!Device?.IsHub ?? true)
        {
            RemoveDuplicateEntries((record) => {
                if (record.IsInUse)
                {
                    // Find the duplicate that we will keep in the database.
                    // Mark it change to prevent it from being removed or replaced.
                    // Make sure the record we keep has a non-zero scene id.
                    int i = IndexOf(record);
                    if (i != -1 && this[i].IsInUse && !ReferenceEquals(this[i], record))
                    {
                        if (record.SceneId != 0)
                            this[i] = new(record) { SyncStatus = SyncStatus.Changed };
                        else
                            this[i] = new(this[i]) { SyncStatus = SyncStatus.Changed };
                    }

                    // Remove the duplicate
                    Replace(record.Uid, new(record) { IsInUse = false, SyncStatus = SyncStatus.Changed });
                }
            });
        }
    }

    /// <summary>
    /// Remove entries for deleted records.
    /// For UnitTest only, this can create a major write operation to the device database.
    /// </summary>
    public void Compress()
    {
        var recordsToDelete = new List<AllLinkRecord>();
        foreach(var record in this.Where(r => !r.IsInUse && !r.IsHighWatermark).ToList())
        {
            recordsToDelete.Add(record);
        }

        foreach(var record in recordsToDelete)
        {
            RemoveMatchingEntry(record);
        }
    }

    /// <summary>
    /// We know the database has changed from under us (e.g., this device was added or,
    /// this is the hub and a device was added).
    /// Force re-reading the database from the physical device.
    /// </summary>
    public void ResetSyncStatus()
    {
        // Indicate that the database is likely to need to be synced with the physical device
        if (LastStatus == SyncStatus.Synced) LastStatus = SyncStatus.Unknown;

        // Force re-reading the database
        IsRead = false;
    }

    /// <summary>
    /// Mark all records as changed.
    /// This is needed if we assign a whole new database to a device (e.g., when we replace or copy a device)
    /// </summary>
    public void MarkAllRecordsChanged()
    {
        for (int index = 0; index < Count; index++)
        {
            UpdateRecordSyncStatus(index, SyncStatus.Changed);
        }
        LastStatus = SyncStatus.Changed;
    }
}
