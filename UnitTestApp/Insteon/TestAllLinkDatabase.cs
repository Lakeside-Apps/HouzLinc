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
using Insteon.Mock;
using Insteon.Model;
using Insteon.Drivers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Insteon;

[TestClass]
public sealed class TestAllLinkDatabase
{
    private static TestContext? TestContext;

    [ClassInitialize]
    public static void SetupTests(TestContext testContext)
    {
        TestContext = testContext;
    }
    
    private AllLinkDatabase BuildMainDatabase()
    {
        AllLinkDatabase allLinkDatabase = new AllLinkDatabase();

        // 0
        allLinkDatabase.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\" modified=\"6/22/2014 10:00:00 AM\""));
        // 1
        allLinkDatabase.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"11\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"4\" modified=\"11/28/2014 3:53:23 PM\""));
        // 2
        allLinkDatabase.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"2\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"4\" modified=\"10/19/2014 2:40:31 PM\""));
        // 3
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.FD.91\" group=\"4\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"4\""));
        // 4
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F3.E4\" group=\"5\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" modified=\"10/19/2014 2:24:10 PM\""));
        // 5
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" modified=\"10/19/2014 2:24:10 PM\""));
        // 6
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" modified=\"10/19/2014 2:24:10 PM\""));
        // 7
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" modified=\"10/19/2014 2:24:10 PM\""));
        // 8
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.65.56\" group=\"1\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"1\" modified=\"10/19/2014 9:04:45 PM\""));
        // 9
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.FD.91\" group=\"1\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"4\" modified=\"10/19/2014 2:40:45 PM\""));
        // 10
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" modified=\"10/19/2014 2:24:10 PM\""));
        
        // Non-sequential writing simulating acquiring records from the physical device
        // 12
        allLinkDatabase.SetRecordAt(12, (new AllLinkRecord("address=\"22.F9.59\" group=\"4\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"4\" modified=\"10/19/2014 2:40:51 PM\"")));
        // 11
        allLinkDatabase.SetRecordAt(11, (new AllLinkRecord("address=\"22.F3.E4\" group=\"5\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" modified=\"10/19/2014 2:24:10 PM\"")));
        // 14
        allLinkDatabase.SetRecordAt(14, (new AllLinkRecord("address=\"22.F6.B8\" group=\"3\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"4\" modified=\"10/19/2014 2:40:54 PM\"")));
        // 13
        allLinkDatabase.SetRecordAt(13, (new AllLinkRecord("address=\"22.F3.7F\" group=\"1\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" modified=\"10/19/2014 2:24:10 PM\"")));

        // 15 High water mark
        allLinkDatabase.Add(AllLinkRecord.CreateHighWaterMark());

        return allLinkDatabase;
    }

    [TestMethod]
    public void TestAllLinkDatabase_Add()
    {
        AllLinkDatabase allLinkDatabase = BuildMainDatabase();
        Assert.IsTrue(allLinkDatabase.Count == 16);
        Assert.IsTrue(allLinkDatabase[3].Equals(new AllLinkRecord("address=\"22.FD.91\" group=\"4\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"4\"")));
        Assert.IsTrue(allLinkDatabase[11].Equals(new AllLinkRecord("address=\"22.F3.E4\" group=\"5\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" modified=\"10/19/2014 2:24:10 PM\"")));
        Assert.IsTrue(allLinkDatabase[12].Equals(new AllLinkRecord("address=\"22.F9.59\" group=\"4\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"4\" modified=\"10/19/2014 2:40:51 PM\"")));
        Assert.IsTrue(allLinkDatabase[13].Equals(new AllLinkRecord("address=\"22.F3.7F\" group=\"1\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" modified=\"10/19/2014 2:24:10 PM\"")));
        Assert.IsTrue(allLinkDatabase[14].Equals(new AllLinkRecord("address=\"22.F6.B8\" group=\"3\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"4\" modified=\"10/19/2014 2:40:54 PM\"")));
    }

    [TestMethod]
    public void TestAllLinkDatabase_Lookup_FullLink()
    {
        AllLinkDatabase allLinkDatabase = BuildMainDatabase();
        // Look-up ignore data fields
        AllLinkRecord record = new AllLinkRecord("address=\"22.FD.91\" group=\"1\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"4\" modified=\"10/19/2014 2:40:45 PM\"");
        Assert.IsTrue(allLinkDatabase.Contains(record));
        Assert.IsTrue(allLinkDatabase.TryGetEntry(record, null, out AllLinkRecord? matchingRecordInList));
        Assert.AreEqual(matchingRecordInList, allLinkDatabase[9]);
    }

    [TestMethod]
    public void TestAllLinkDatabase_Lookup_FullLink_With_Duplicate_InsteonId()
    {
        AllLinkDatabase allLinkDatabase = BuildMainDatabase();
        // Look up first of a pair of links sharing same insteon ID
        AllLinkRecord record = new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" modified=\"10/19/2014 2:24:10 PM\"");
        Assert.IsTrue(allLinkDatabase.Contains(record));
        Assert.IsTrue(allLinkDatabase.TryGetEntry(record, null, out AllLinkRecord? matchingRecordInList));
        Assert.AreEqual(matchingRecordInList, allLinkDatabase[6]);

        // Look up second of a pair of links sharing same insteon ID
        record = new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" modified=\"10/19/2014 2:24:10 PM\"");
        Assert.IsTrue(allLinkDatabase.Contains(record));
        Assert.IsTrue(allLinkDatabase.TryGetEntry(record, null, out matchingRecordInList));
        Assert.AreEqual(matchingRecordInList, allLinkDatabase[7]);
    }

    [TestMethod]
    public void TestAllLinkDatabase_Lookup_ByInsteonId()
    {
        AllLinkDatabase allLinkDatabase = BuildMainDatabase();
        // Look-up links by insteon ID
        Assert.IsTrue(allLinkDatabase.Contains(Device.GetHashCodeFromId(new InsteonID("22.F6.B8"))));
        Assert.IsTrue(allLinkDatabase.TryGetMatchingEntries(Device.GetHashCodeFromId(new InsteonID("22.F6.B8")), out List<AllLinkRecord>? matchingRecords));
        Assert.AreEqual(matchingRecords.Count, 3);
        Assert.AreEqual(matchingRecords[0], allLinkDatabase[6]);
        Assert.AreEqual(matchingRecords[1], allLinkDatabase[7]);
        Assert.AreEqual(matchingRecords[2], allLinkDatabase[14]);
    }

    [TestMethod]
    public void TestAllLinkDatabase_AddDuplicate()
    {
        AllLinkDatabase allLinkDatabase = BuildMainDatabase();
        // Add a duplicate
        AllLinkRecord allLinkRecord = new AllLinkRecord("address=\"22.65.56\" group=\"1\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"1\" modified=\"10/19/2014 9:04:45 PM\"");
        allLinkDatabase.AddRecord(allLinkRecord);

        // Now lookup and verify response
        Assert.IsTrue(allLinkDatabase.TryGetMatchingEntries(allLinkRecord.GetHashCode(), out List<AllLinkRecord>? matchingRecords));
        Assert.AreEqual(matchingRecords.Count, 2);
        Assert.AreEqual(matchingRecords[0], allLinkDatabase[8]);
        Assert.AreEqual(matchingRecords[1], allLinkDatabase[15]);
    }

    [TestMethod]
    public void TestAllLinkDatase_RemoveUniqueLinkForKey()
    {
        AllLinkDatabase allLinkDatabase = BuildMainDatabase();
        Assert.IsTrue(allLinkDatabase.Remove(allLinkDatabase[12]));
        Assert.IsFalse(allLinkDatabase.Contains(new AllLinkRecord("address=\"22.F9.59\" group=\"4\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"4\" modified=\"10/19/2014 2:40:51 PM\"")));
        Assert.IsFalse(allLinkDatabase.Contains(Device.GetHashCodeFromId(new InsteonID("22.F9.59"))));
    }

    [TestMethod]
    public void TestAllLinkDatase_RemoveNotUniqueLinkForKey()
    {
        AllLinkDatabase allLinkDatabase = BuildMainDatabase();
        Assert.IsTrue(allLinkDatabase.Remove(allLinkDatabase[7]));
        Assert.IsFalse(allLinkDatabase.Contains(new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" modified=\"10/19/2014 2:24:10 PM\"")));
        Assert.IsTrue(allLinkDatabase.Contains(Device.GetHashCodeFromId(new InsteonID("22.F9.59"))));
    }

    private AllLinkDatabase BuildDatabaseBefore()
    {
        AllLinkDatabase allLinkDatabase = new AllLinkDatabase();

        // 0
        allLinkDatabase.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\" uid=\"1\""));
        // 1
        allLinkDatabase.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"2\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"4\" uid=\"2\""));
        // 2 - Not in use
        allLinkDatabase.Add(new AllLinkRecord("address=\"11.11.11\" group=\"4\" recordControl=\"34\" data1=\"255\" data2=\"28\" data3=\"6\" uid=\"3\""));
        // 3
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" uid=\"4\" syncstatus=\"changed\""));
        // 4 - Not in use
        allLinkDatabase.Add(new AllLinkRecord("address=\"11.11.11\" group=\"1\" recordControl=\"34\" data1=\"255\" data2=\"28\" data3=\"4\" uid=\"5\""));
        // 5 - High water mark 
        allLinkDatabase.Add(AllLinkRecord.CreateHighWaterMark(uid: 1000));

        return allLinkDatabase;
    }

    private AllLinkDatabase BuildReferenceDatabaseAfterAdd()
    {
        AllLinkDatabase allLinkDatabase = new AllLinkDatabase();

        // 0
        allLinkDatabase.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\" uid=\"1\""));
        // 1
        allLinkDatabase.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"2\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"4\" uid=\"2\""));
        // 2 - Reused
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" uid=\"100\" syncstatus=\"changed\""));
        // 3
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" uid=\"4\" syncstatus=\"changed\""));
        // 4 - Reused
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F3.E4\" group=\"5\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" uid=\"101\" syncstatus=\"changed\""));
        // 5 - Added at the end
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" uid=\"102\""));
        // 6 - High water mark 
        allLinkDatabase.Add(AllLinkRecord.CreateHighWaterMark(uid: 1000, SyncStatus.Unknown)); // picks up the sync status of last added record

        return allLinkDatabase;
    }

    [TestMethod]
    public void TestAllLinkDatabase_AddRecordsToDatabase()
    {
        AllLinkDatabase database = BuildDatabaseBefore();
        AllLinkDatabase referenceDatabase = BuildReferenceDatabaseAfterAdd();

        database.AddRecord(new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" syncstatus=\"changed\" uid=\"100\""));
        database.AddRecord(new AllLinkRecord("address=\"22.F3.E4\" group=\"5\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" syncstatus=\"changed\" uid=\"101\""));
        database.AddRecord(new AllLinkRecord("address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" syncstatus=\"unknown\" uid=\"102\""));

        // Check records against reference
        Assert.AreEqual(referenceDatabase.Count, database.Count, "Test and ref databases have a different number of records");
        for (int i = 0; i < database.Count; i++)
        {
            Assert.AreEqual(referenceDatabase[i], database[i], "Record " + i + ": test different from ref");
            Assert.AreEqual(referenceDatabase[i].Uid, database[i].Uid, "Record " + i + ": test Uid different from ref");
            Assert.AreEqual(referenceDatabase[i].SyncStatus, database[i].SyncStatus, "Record " + i + ": test sync status different from ref");
        }
    }

    private AllLinkDatabase BuildReferenceDatabaseAfterReplace()
    {
        AllLinkDatabase allLinkDatabase = new AllLinkDatabase();

        // 0 - Replaced
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" syncstatus=\"changed\" uid=\"100\""));
        // 1 - Replaced
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.22.22\" group=\"8\" recordControl=\"226\" data1=\"255\" data2=\"28\" data3=\"8\" uid=\"101\" syncstatus=\"synced\""));
        // 2 - Not in use
        allLinkDatabase.Add(new AllLinkRecord("address=\"11.11.11\" group=\"4\" recordControl=\"34\" data1=\"255\" data2=\"28\" data3=\"6\" uid=\"3\""));
        // 3
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" uid=\"4\" syncstatus=\"changed\""));
        // 4 - Replaced not in use record
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F3.E4\" group=\"5\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" uid=\"102\"  syncstatus=\"unknown\""));
        // 5 - High water mark 
        allLinkDatabase.Add(AllLinkRecord.CreateHighWaterMark(uid: 1000));

        return allLinkDatabase;
    }

    [TestMethod]
    public void TestAllLinkDatabase_ReplaceRecord()
    {
        AllLinkDatabase database = BuildDatabaseBefore();
        AllLinkDatabase referenceDatabase = BuildReferenceDatabaseAfterReplace();

        database.ReplaceRecord(database[0], new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" uid=\"100\" syncstatus=\"changed\""));
        database.ReplaceRecord(database[1], new AllLinkRecord("address=\"22.22.22\" group=\"8\" recordControl=\"226\" data1=\"255\" data2=\"28\" data3=\"8\" uid=\"101\" syncstatus=\"synced\""));
        database.ReplaceRecord(database[4], new AllLinkRecord("address=\"22.F3.E4\" group=\"5\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" uid=\"102\""));

        // Check records against reference
        Assert.AreEqual(referenceDatabase.Count, database.Count, "Test and ref databases have a different number of records");
        for (int i = 0; i < database.Count; i++)
        {
            Assert.AreEqual(referenceDatabase[i], database[i], "Record " + i + ": test different from ref");
            Assert.AreEqual(referenceDatabase[i].Uid, database[i].Uid, "Record " + i + ": test Uid different from ref");
            Assert.AreEqual(referenceDatabase[i].SyncStatus, database[i].SyncStatus, "Record " + i + ": test sync status different from ref");
        }
    }

    private AllLinkDatabase BuildReferenceDatabaseAfterSwap()
    {
        AllLinkDatabase allLinkDatabase = new AllLinkDatabase();

        // 0
        allLinkDatabase.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"2\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"4\" uid=\"2\" syncstatus=\"changed\""));
        // 1
        allLinkDatabase.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\" uid=\"1\" syncstatus=\"changed\""));
        // 2 
        allLinkDatabase.Add(new AllLinkRecord("address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" uid=\"4\" syncstatus=\"changed\""));
        // 3 - Not in use
        allLinkDatabase.Add(new AllLinkRecord("address=\"11.11.11\" group=\"4\" recordControl=\"34\" data1=\"255\" data2=\"28\" data3=\"6\" uid=\"3\" syncstatus=\"changed\""));
        // 4 - Not in use
        allLinkDatabase.Add(new AllLinkRecord("address=\"11.11.11\" group=\"1\" recordControl=\"34\" data1=\"255\" data2=\"28\" data3=\"4\" uid=\"5\""));
        // 5 - High water mark 
        allLinkDatabase.Add(AllLinkRecord.CreateHighWaterMark(uid: 1000));

        return allLinkDatabase;
    }

    [TestMethod]
    public void TestAllLinkDatabase_SwapRecords()
    {
        AllLinkDatabase database = BuildDatabaseBefore();
        AllLinkDatabase referenceDatabase = BuildReferenceDatabaseAfterSwap();

        Assert.IsTrue(database.SwapRecords(0, 1));
        Assert.IsTrue(database.SwapRecords(2, 3));
        Assert.IsFalse(database.SwapRecords(3, 5)); // attempt to swap with high water mark, should fail

        // Check records against reference
        Assert.AreEqual(referenceDatabase.Count, database.Count, "Test and ref databases have a different number of records");
        for (int i = 0; i < database.Count; i++)
        {
            Assert.AreEqual(referenceDatabase[i], database[i], "Record " + i + ": test different from ref");
            Assert.AreEqual(referenceDatabase[i].Uid, database[i].Uid, "Record " + i + ": test Uid different from ref");
            Assert.AreEqual(referenceDatabase[i].SyncStatus, database[i].SyncStatus, "Record " + i + ": test sync status different from ref");
        }
    }

    //
    // Testing reading and writing (syncing) databases on regular devices
    //
    // Expected results of read and write database on regular devices
    // These databases are ordered, order is preserved, they are compared line by line, one line at a time
    //

    //    | Original Logical     | Original Physical       | Logical after Read (Merge)  | Logical after Write               | Physical after Write    |
    // ---|----------------------|-------------------------|-----------------------------|-----------------------------------|-------------------------|
    // 0  | In use, changed      | Matches, in use         | Marked synced               | Marked synced                     | No change               |
    // 1  | In use, synced       | Matches, in use         | No change                   | No change                         | No change               |
    // 2  | In use, changed      | Different, in use       | No change                   | Marked synced                     | Logical record          |
    // 3  | In use, synced       | Different, in use       | Physical record, synced     | Physical record, synced           | No change               |
    // 4  | In use, changed      | Partial Match, in use   | No change                   | No change                         | logical record          |
    // 5  | In use, synced       | Partial Match, in use   | Physical record, synced     | Physical record, synced           | No change               |
    // 6  | Not in use, changed  | Any, in use             | No change                   | Marked synced                     | Not in use              |
    // 7  | Not in use, synced   | Any, in use             | Physical record, synced     | Physical record, synced           | No change               |
    // 8  | Not in use, changed  | Any, not in use         | No change                   | Marked synced                     | No change (not in use)  |
    // 9  | Not in use, synced   | Any, not in use         | No change                   | No change                         | No change (not in use)  |
    // 10 | In use, changed      | Any, not in used        | No change                   | Marked synced                     | Logical version         |
    // 11 | In use, synced       | Any, not in used        | Not in use, synced          | Marked not in use, synced         | Not in use              |
    // 12 | In use, changed      | Past EOD                | No change                   | Marked synced                     | Logical version         |
    // 13 | In use, synced       | Past EOD                | Removed                     | Removed                           | Past EOD                |
    // 14 | Not in use, changed  | Past EOD                | Removed                     | Removed                           | Past EOD                |
    // 15 | Not in use, synced   | Past EOD                | Removed                     | Removed                           | Past EOD                |
    // 16 | Past EOD             | In use                  | Physical record, synced     | Physical record, in use, synced   | No change               |
    // 17 | Past EOD             | Not in use              | Past EOD                    | Past EOD                          | Removed                 |

    // Notes:
    // - Unknown status in the logical database is treated same as changed
    // - Changed status propagates the logical record to the physical database
    // - Sync status gets the record replaced in the logical database by the physical database record

    private (AllLinkDatabase logicalDatabase, AllLinkDatabase physicalDatabase, AllLinkDatabase expectedLogicalDatabaseAfterRead, AllLinkDatabase expectedLogicalDatabaseAfterWrite, AllLinkDatabase expectedPhysicalDatabaseAfterWrite) BuildDatabases_Core()
    {
        var logicalDatabase = new AllLinkDatabase();
        var physicalDatabase = new AllLinkDatabase();
        var expectedLogicalDatabaseAfterRead = new AllLinkDatabase();
        var expectedLogicalDatabaseAfterWrite = new AllLinkDatabase();
        var expectedPhysicalDatabaseAfterWrite = new AllLinkDatabase();

        // 0 - Changed
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\" syncstatus=\"changed\" sceneid=\"1\""));
        // Matches logical
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\""));
        // Keep in logical after read
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\" syncstatus=\"synced\" sceneid=\"1\""));
        // Keep in logical after write, mark synced
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\" syncstatus=\"synced\" sceneid=\"1\""));
        // No change to pyhsical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\""));

        // 1 - Synced
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"1E.C3.EA\" group=\"2\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"4\" syncstatus=\"synced\" sceneid=\"2\" "));
        // Matches physical
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"1E.C3.EA\" group=\"2\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"4\""));
        // No change in physical database after read
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"1E.C3.EA\" group=\"2\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"4\" syncstatus=\"synced\" sceneid=\"2\""));
        // No change afer write
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"1E.C3.EA\" group=\"2\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"4\" syncstatus=\"synced\" sceneid=\"2\""));
        // No change to physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"2\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"4\""));

        // 2 - Changed
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" syncstatus=\"changed\" sceneid=\"3\""));
        // Partial match in physical
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"160\" data2=\"28\" data3=\"6\""));
        // LogicalDatabase version should be in merge after read, still marked changed
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" syncstatus=\"changed\" sceneid=\"3\""));
        // LogicalDatabase version should be in logical after read, still marked changed
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"3\""));
        // LogicalDatabase version should be written to physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\""));

        // 3 - Synced
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"1E.C3.EA\" group=\"3\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"4\" syncstatus=\"synced\" sceneid=\"2\" "));
        // Partial match in physical
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"1E.C3.EA\" group=\"3\" recordControl=\"162\" data1=\"150\" data2=\"28\" data3=\"4\""));
        // PhysicalDatabase version should be in merge, marked synced
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"1E.C3.EA\" group=\"3\" recordControl=\"162\" data1=\"150\" data2=\"28\" data3=\"4\" syncstatus=\"synced\" sceneid=\"0\""));
        // PhysicalDatabase version should be in logical, marked synced
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"1E.C3.EA\" group=\"3\" recordControl=\"162\" data1=\"150\" data2=\"28\" data3=\"4\" syncstatus=\"synced\" sceneid=\"0\""));
        // No change in physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"3\" recordControl=\"162\" data1=\"150\" data2=\"28\" data3=\"4\""));

        // 4 - Changed
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" syncstatus=\"changed\" sceneid=\"3\""));
        // Different from LogicalDatabase (Data3 different on responder record)
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"3\""));
        // LogicalDatabase record should be in merge, still marked changed
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" syncstatus=\"changed\" sceneid=\"3\""));
        // LogicalDatabase version should be in logical, marked synced
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"3\""));
        // LogicalDatabase version should be written to physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\""));

        // 5 - Synced
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"22.F5.67\" group=\"3\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"4\" syncstatus=\"synced\" sceneid=\"2\" "));
        // Different from LogicalDatabase (completely different)
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"23.44.EA\" group=\"0\" recordControl=\"162\" data1=\"150\" data2=\"28\" data3=\"4\""));
        // PhysicalDatabase version should be in logical, marked synced
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"23.44.EA\" group=\"0\" recordControl=\"162\" data1=\"150\" data2=\"28\" data3=\"4\" syncstatus=\"synced\" sceneid=\"0\""));
        // PhysicalDatabase version should be in merge, marked synced
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"23.44.EA\" group=\"0\" recordControl=\"162\" data1=\"150\" data2=\"28\" data3=\"4\" syncstatus=\"synced\" sceneid=\"0\""));
        // No change to physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"23.44.EA\" group=\"0\" recordControl=\"162\" data1=\"150\" data2=\"28\" data3=\"4\""));

        // 6 - Deleted (not in use, changed)
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"22.FD.91\" group=\"4\" recordControl=\"98\" data1=\"3\" data2=\"28\" data3=\"4\" syncstatus=\"changed\" sceneid=\"4\""));
        // In use in physical database
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.FD.91\" group=\"4\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"4\""));
        // Should still be deleted in logical, still marked changed
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.FD.91\" group=\"4\" recordControl=\"98\" data1=\"3\" data2=\"28\" data3=\"4\" syncstatus=\"changed\" sceneid=\"4\""));
        // Should still be marked not in used, synced in logical
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.FD.91\" group=\"4\" recordControl=\"98\" data1=\"3\" data2=\"28\" data3=\"4\" syncstatus=\"synced\" sceneid=\"4\""));
        // And marked not in used in physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.FD.91\" group=\"4\" recordControl=\"98\" data1=\"3\" data2=\"28\" data3=\"4\""));

        // 7 - Not in used, sycned
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"22.F2.26\" group=\"0\" recordControl=\"34\" data1=\"255\" data2=\"28\" data3=\"8\" syncstatus=\"synced\" sceneid=\"5\""));
        // In use in physical database
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.F3.E4\" group=\"5\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"5\""));
        // Physical record should be in logical after read, marked synced
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.F3.E4\" group=\"5\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"5\" syncstatus=\"synced\" sceneid=\"0\""));
        // No change after write
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.F3.E4\" group=\"5\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"5\" syncstatus=\"synced\" sceneid=\"0\""));
        // no change in physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.F3.E4\" group=\"5\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"5\""));

        // 8 - Deleted (Not in use, changed)
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"22.F3.E5\" group=\"6\" recordControl=\"34\" data1=\"140\" data2=\"28\" data3=\"6\" syncstatus=\"changed\" sceneid=\"6\""));
        // Not in use
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.F3.E5\" group=\"6\" recordControl=\"34\" data1=\"140\" data2=\"28\" data3=\"6\""));
        // Keep LogicalDatabase version in logical, mark synced
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.F3.E5\" group=\"6\" recordControl=\"34\" data1=\"140\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"6\""));
        // Keep in LogicalDatabase, mark synced
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.F3.E5\" group=\"6\" recordControl=\"34\" data1=\"140\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"6\""));
        // And keep in physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.F3.E5\" group=\"6\" recordControl=\"34\" data1=\"140\" data2=\"28\" data3=\"6\""));

        // 9 - Not in use, synced
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"22.45.D3\" group=\"1\" recordControl=\"34\" data1=\"255\" data2=\"28\" data3=\"4\" syncstatus=\"synced\" sceneid=\"7\""));
        // Not in use in physical database
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.FD.91\" group=\"1\" recordControl=\"34\" data1=\"45\" data2=\"28\" data3=\"6\" "));
        // Propagate physical record to logical after read
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.FD.91\" group=\"1\" recordControl=\"34\" data1=\"45\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"0\""));
        // keep logical database unchanged after write
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord(  "address=\"22.FD.91\" group=\"1\" recordControl=\"34\" data1=\"45\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"0\""));
        // Keep physical database unchanged
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.FD.91\" group=\"1\" recordControl=\"34\" data1=\"45\" data2=\"28\" data3=\"6\" "));

        // 10 - Changed
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"23.44.AB\" group=\"3\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"3\" syncstatus=\"changed\" sceneid=\"2\" "));
        // Not in use in physical database
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.56.E1\" group=\"4\" recordControl=\"34\" data1=\"140\" data2=\"28\" data3=\"6\""));
        // Keep in logical after read and mark changed
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"23.44.AB\" group=\"3\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"3\" syncstatus=\"changed\" sceneid=\"2\" "));
        // Keep in logical after write and mark synced
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"23.44.AB\" group=\"3\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"3\" syncstatus=\"synced\" sceneid=\"2\" "));
        // Write logical version to physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"23.44.AB\" group=\"3\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"3\""));

        // 11 - Synced
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"2E.DF.22\" group=\"3\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"3\" syncstatus=\"synced\" sceneid=\"2\" "));
        // Not in use in physical database
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.AB.23\" group=\"4\" recordControl=\"34\" data1=\"140\" data2=\"28\" data3=\"6\""));
        // Physical version should be propagated to logical and marked changed
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.AB.23\" group=\"4\" recordControl=\"34\" data1=\"140\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"0\" "));
        // Physical should be propagated to merge and marked synced
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.AB.23\" group=\"4\" recordControl=\"34\" data1=\"140\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"0\""));
        // No change in physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.AB.23\" group=\"4\" recordControl=\"34\" data1=\"140\" data2=\"28\" data3=\"6\""));

        return (logicalDatabase, 
                physicalDatabase, 
                expectedLogicalDatabaseAfterRead, 
                expectedLogicalDatabaseAfterWrite, 
                expectedPhysicalDatabaseAfterWrite);
    }

    private (AllLinkDatabase logicalDatabase, AllLinkDatabase physicalDatabase, AllLinkDatabase expectedLogicalDatabaseAfterRead, AllLinkDatabase expectedLogicalDatabaseAfterWrite, AllLinkDatabase expectedPhysicalDatabaseAfterWrite) BuildDatabases_ShorterLogical()
    {
        (var logicalDatabase, var physicalDatabase, var expectedLogicalDatabaseAfterRead, var expectedLogicalDatabaseAfterWrite, var expectedPhysicalDatabaseAfterWrite) = BuildDatabases_Core();

        // 12 - High water mark 
        logicalDatabase.Add(AllLinkRecord.CreateHighWaterMark());
        // Does not exist in LogicalDatabase
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\""));
        // Should be added to merge and marked synced
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" syncstatus=\"synced\" sceneid=\"0\""));
        // Should be marked synced
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" syncstatus=\"synced\" sceneid=\"0\""));
        // Keep in physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\""));

        // 13 - Does not exist in LogicalDatabase
        //
        // Exist in physical database
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\""));
        // Should be added to merge and marked synced
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"0\""));
        // Should be marked synced
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"0\""));
        // Retain as is in physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\""));

        // 14 - Does not exist in LogicalDatabase
        //
        // High water mark 
        physicalDatabase.Add(AllLinkRecord.CreateHighWaterMark());
        // High water mark 
        expectedLogicalDatabaseAfterRead.Add(AllLinkRecord.CreateHighWaterMark(SyncStatus.Synced));
        // High water mark 
        expectedLogicalDatabaseAfterWrite.Add(AllLinkRecord.CreateHighWaterMark(SyncStatus.Synced));
        // High water mark 
        expectedPhysicalDatabaseAfterWrite.Add(AllLinkRecord.CreateHighWaterMark());

        return (logicalDatabase, 
                physicalDatabase, 
                expectedLogicalDatabaseAfterRead,
                expectedLogicalDatabaseAfterWrite, 
                expectedPhysicalDatabaseAfterWrite);
    }

    private (AllLinkDatabase logicalDatabase, AllLinkDatabase physicalDatabase, AllLinkDatabase expectedLogicalDatabaseAfterRead, AllLinkDatabase expectedLogicalDatabaseAfterWrite, AllLinkDatabase expectedPhysicalDatabaseAfterWrite) BuildDatabases_ShorterPhysical()
    {
        (var logicalDatabase, var physicalDatabase, var expectedLogicalDatabaseAfterRead, var expectedLogicalDatabaseAfterWrite, var expectedPhysicalDatabaseAfterWrite) = BuildDatabases_Core();

        // 12 - Deleted record (not in use, changed)
        logicalDatabase.Add(new AllLinkRecord("address=\"22.85.11\" group=\"1\" recordControl=\"34\" data1=\"3\" data2=\"28\" data3=\"1\" syncstatus=\"changed\" sceneid=\"7\""));
        // Past the end of physical database
        //
        // Removed from logical after read
        //
        // Still removed from logical after write
        //
        // Past EOD
        //

        // 13 - New record, marked changed
        logicalDatabase.Add(new AllLinkRecord(                    "address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" syncstatus=\"changed\" sceneid=\"4\""));
        // Does not exist in physical database (High water mark)
        physicalDatabase.Add(AllLinkRecord.CreateHighWaterMark());
        // New record should be added to the merge, marked changed (will be written later)
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" syncstatus=\"changed\" sceneid=\"4\""));
        // New record should be added to the merge, marked synced
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" syncstatus=\"synced\" sceneid=\"4\""));
        // And written to the physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\""));

        // 14 - New record, marked synced
        logicalDatabase.Add(new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"0\""));
        // Past the end of physical database
        //
        // Removed from logical after read, marked synced
        //
        // Still removed from logical after write
        //
        // Past EOD
        //

        // 15 - Deleted record (not in use, changed)
        logicalDatabase.Add(new AllLinkRecord("address=\"22.7D.32\" group=\"1\" recordControl=\"34\" data1=\"255\" data2=\"28\" data3=\"4\" syncstatus=\"changed\" sceneid=\"7\""));
        // Past the end of physical database
        //
        // Should not be in logical after read. High water mark
        expectedLogicalDatabaseAfterRead.Add(AllLinkRecord.CreateHighWaterMark(SyncStatus.Changed));
        // Should not be in the logical merge. High water mark
        expectedLogicalDatabaseAfterWrite.Add(AllLinkRecord.CreateHighWaterMark(SyncStatus.Synced));
        // Should not be in physical database. High water mark
        expectedPhysicalDatabaseAfterWrite.Add(AllLinkRecord.CreateHighWaterMark());

        return (logicalDatabase, 
                physicalDatabase, 
                expectedLogicalDatabaseAfterRead,
                expectedLogicalDatabaseAfterWrite, 
                expectedPhysicalDatabaseAfterWrite);
    }

    [TestMethod]
    public async Task TestAllLinkDatabase_ReadDatabase_ShorterLogical()
    {
        (var logicalDatabase, var physicalDatabase, var expectedDatabase, _, _) = BuildDatabases_ShorterLogical();

        // This changes logicalDatabase, which we will check against expectedDatabase
        var house = new House()
            .WithGateway()
            .WithMockPhysicalDevice(new MockPhysicalDevice(InsteonID.Null, physicalDatabase));
        var deviceDriver = new DeviceDriver(house, InsteonID.Null);
        await deviceDriver.TryReadAllLinkDatabaseAsync(logicalDatabase);

        CompareDatabases(logicalDatabase, expectedDatabase);
    }

    [TestMethod]
    public async Task TestAllLinkDatabase_ReadDatabase_ShorterPhysical()
    {
        (var logicalDatabase, var physicalDatabase, var expectedDatabase, _, _) = BuildDatabases_ShorterPhysical();

        // This changes logicalDatabase, which we will check against expectedDatabase
        var house = new House()
            .WithGateway()
            .WithMockPhysicalDevice(new MockPhysicalDevice(InsteonID.Null, physicalDatabase));
        var deviceDriver = new DeviceDriver(house, InsteonID.Null);
        await deviceDriver.TryReadAllLinkDatabaseAsync(logicalDatabase);

        CompareDatabases(logicalDatabase, expectedDatabase);
    }

    [TestMethod]
    public async Task TestAllLinkDatabase_ReadDatabase_ShorterLogical_UnknownStatuses()
    {
        (var logicalDatabase, var physicalDatabase, var expectedDatabase, _, _) = BuildDatabases_ShorterLogical();

        ReplaceChangedByUnknown(logicalDatabase);

        // This changes logicalDatabase, which we will check against expectedDatabase
        var house = new House()
            .WithGateway()
            .WithMockPhysicalDevice(new MockPhysicalDevice(InsteonID.Null, physicalDatabase));
        var deviceDriver = new DeviceDriver(house, InsteonID.Null);
        await deviceDriver.TryReadAllLinkDatabaseAsync(logicalDatabase);

        CompareDatabases(logicalDatabase, expectedDatabase);
    }

    [TestMethod]
    public async Task TestAllLinkDatabase_ReadDatabase_ShorterPhysical_UnknownStatuses()
    {
        (var logicalDatabase, var physicalDatabase, var expectedDatabase, _, _) = BuildDatabases_ShorterPhysical();

        ReplaceChangedByUnknown(logicalDatabase);

        // This changes logicalDatabase, which we will check against expectedDatabase
        var house = new House()
            .WithGateway()
            .WithMockPhysicalDevice(new MockPhysicalDevice(InsteonID.Null, physicalDatabase));
        var deviceDriver = new DeviceDriver(house, InsteonID.Null);
        await deviceDriver.TryReadAllLinkDatabaseAsync(logicalDatabase);

        CompareDatabases(logicalDatabase, expectedDatabase);
    }

    [TestMethod]
    public async Task TestAllLinkDatase_WriteDatabase_ShorterLogical()
    {
        (var logicalDatabase, var physicalDatabase, var _, var expectedLogicalDatabase, var expectedPhysicalDatabase) = BuildDatabases_ShorterLogical();

        // This changes both logicalDatabase and physicalDatabase, which we will check against expected
        var house = new House()
            .WithGateway()
            .WithMockPhysicalDevice(new MockPhysicalDevice(InsteonID.Null, physicalDatabase));
        var deviceDriver = new DeviceDriver(house, InsteonID.Null);
        await deviceDriver.TryWriteAllLinkDatabaseAsync(logicalDatabase);

        CompareDatabases(logicalDatabase, expectedLogicalDatabase);
        CompareDatabases(physicalDatabase, expectedPhysicalDatabase, header: "Physical", ignoreSyncStatus: true, ignoreSceneId: true);
    }

    [TestMethod]
    public async Task TestAllLinkDatase_WriteDatabase_ShorterPhysical()
    {
        (var logicalDatabase, var physicalDatabase, var _, var expectedLogicalDatabase, var expectedPhysicalDatabase) = BuildDatabases_ShorterPhysical();

        // This changes both logicalDatabase and physicalDatabase, which we will check against expected
        var house = new House()
            .WithGateway()
            .WithMockPhysicalDevice(new MockPhysicalDevice(InsteonID.Null, physicalDatabase));
        var deviceDriver = new DeviceDriver(house, InsteonID.Null);
        await deviceDriver.TryWriteAllLinkDatabaseAsync(logicalDatabase);

        CompareDatabases(logicalDatabase, expectedLogicalDatabase);
        CompareDatabases(physicalDatabase, expectedPhysicalDatabase, header: "Physical", ignoreSyncStatus: true, ignoreSceneId: true);
    }

    [TestMethod]
    public async Task TestAllLinkDatase_WriteDatabase_ShorterLogical_UnknownStatuses()
    {
        (var logicalDatabase, var physicalDatabase, var _, var expectedLogicalDatabase, var expectedPhysicalDatabase) = BuildDatabases_ShorterLogical();

        ReplaceChangedByUnknown(logicalDatabase);

        // This changes both logicalDatabase and physicalDatabase, which we will check against expected
        var house = new House()
            .WithGateway()
            .WithMockPhysicalDevice(new MockPhysicalDevice(InsteonID.Null, physicalDatabase));
        var deviceDriver = new DeviceDriver(house, InsteonID.Null);
        await deviceDriver.TryWriteAllLinkDatabaseAsync(logicalDatabase);

        CompareDatabases(logicalDatabase, expectedLogicalDatabase);
        CompareDatabases(physicalDatabase, expectedPhysicalDatabase, header: "Physical", ignoreSyncStatus: true, ignoreSceneId: true);
    }

    [TestMethod]
    public async Task TestAllLinkDatase_WriteDatabase_ShorterPhysical_UnknownStatuses()
    {
        (var logicalDatabase, var physicalDatabase, var _, var expectedLogicalDatabase, var expectedPhysicalDatabase) = BuildDatabases_ShorterPhysical();

        ReplaceChangedByUnknown(logicalDatabase);

        // This changes both logicalDatabase and physicalDatabase, which we will check against expected
        var house = new House()
            .WithGateway()
            .WithMockPhysicalDevice(new MockPhysicalDevice(InsteonID.Null, physicalDatabase));
        var deviceDriver = new DeviceDriver(house, InsteonID.Null);
        await deviceDriver.TryWriteAllLinkDatabaseAsync(logicalDatabase);

        CompareDatabases(logicalDatabase, expectedLogicalDatabase);
        CompareDatabases(physicalDatabase, expectedPhysicalDatabase, header: "Physical", ignoreSyncStatus: true, ignoreSceneId: true);
    }

    // Helper to compare two ordered database record by record
    private void CompareDatabases(AllLinkDatabase database, AllLinkDatabase expectedDatabase, string header = "Logical", bool ignoreSyncStatus = false, bool ignoreSceneId = false)
    {
        // Check that given database has the proper number of records
        if (expectedDatabase.Count != database.Count)
        {
            // Output full dump of both databases
            for (int i = 0; i < database.Count || i < expectedDatabase.Count; i++)
            {
                if (i < expectedDatabase.Count)
                {
                    TestContext?.WriteLine(expectedDatabase[i].GetLogOutput(i, $"{header} ref: ", showNotInUseRecord: true, showSyncStatus: !ignoreSyncStatus, showSceneId: !ignoreSceneId));
                }
                if (i < database.Count)
                {
                    TestContext?.WriteLine(database[i].GetLogOutput(i, $"{header} test:", showNotInUseRecord: true, showSyncStatus: !ignoreSyncStatus, showSceneId: !ignoreSceneId));
                }
            }

            Assert.IsTrue(false, $"{header} test database has {database.Count} record, should have {expectedDatabase.Count}");
        }

        // Check given database against expected database
        for (int i = 0; i < database.Count; i++)
        {
            if (!expectedDatabase[i].Equals(database[i]) ||
                (!ignoreSyncStatus && expectedDatabase[i].SyncStatus != database[i].SyncStatus) ||
                (!ignoreSceneId && expectedDatabase[i].SceneId != database[i].SceneId))
            {
                TestContext?.WriteLine(expectedDatabase[i].GetLogOutput(i, $"{header} ref: ", showNotInUseRecord: true, showSyncStatus: !ignoreSyncStatus, showSceneId: !ignoreSceneId));
                TestContext?.WriteLine(database[i].GetLogOutput(i, $"{header} test:", showNotInUseRecord: true, showSyncStatus: !ignoreSyncStatus, showSceneId: !ignoreSceneId));
                Assert.IsTrue(false, "Record " + i + ": test different from ref");
            }

            // Assert record can be found in the index
            Assert.IsTrue(expectedDatabase.TryGetEntry(expectedDatabase[i], AllLinkRecord.IdGroupTypeComparer.Instance, out AllLinkRecord? record), "Record " + i + ": not found via matching");
            if (!expectedDatabase[i].Equals(database[i]))
            {
                TestContext?.WriteLine(expectedDatabase[i].GetLogOutput(i, $"{header} ref: "));
                TestContext?.WriteLine(record.GetLogOutput(i, $"{header} Test:"));
                Assert.IsTrue(false, "Record " + i + ": test different from ref");
            }
        }
    }

    //
    // Testing reading and writing databases on IM (hub)
    //

    private (AllLinkDatabase logicalDatabase, AllLinkDatabase physicalDatabase, AllLinkDatabase expectedLogicalDatabaseAfterRead, AllLinkDatabase expectedLogicalDatabaseAfterWrite, AllLinkDatabase expectedPhysicalDatabaseAfterWrite) BuildIMDatabases()
    {
        var logicalDatabase = new AllLinkDatabase();
        var physicalDatabase = new AllLinkDatabase();
        var expectedLogicalDatabaseAfterRead = new AllLinkDatabase();
        var expectedLogicalDatabaseAfterWrite = new AllLinkDatabase();
        var expectedPhysicalDatabaseAfterWrite = new AllLinkDatabase();

        // Physical and expected physical database

        // 0
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\""));
        // Full match of physical record, changed status
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\" syncstatus=\"changed\" sceneid=\"1\""));
        // Leave in the logical database, mark synced
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\" syncstatus=\"synced\" sceneid=\"1\""));
        // No change
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\" syncstatus=\"synced\" sceneid=\"1\""));
        // No change to physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"0\" recordControl=\"170\" data1=\"0\" data2=\"28\" data3=\"1\""));

        // 1
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"160\" data2=\"28\" data3=\"7\""));
        // Partial match of physical record, different data1 and data2, changed status
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"30\" data3=\"7\" syncstatus=\"changed\" sceneid=\"3\""));
        // LogicalDatabase version should stay in the logical database after read
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"30\" data3=\"7\" syncstatus=\"changed\" sceneid=\"3\""));
        // Mark synced
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"30\" data3=\"7\" syncstatus=\"synced\" sceneid=\"3\""));
        // Propagate data1 and data2 from logical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.F6.B8\" group=\"4\" recordControl=\"162\" data1=\"255\" data2=\"30\" data3=\"7\""));

        // 2
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"1E.C3.EA\" group=\"3\" recordControl=\"162\" data1=\"150\" data2=\"28\" data3=\"5\""));
        // Partial match of physical record, different in data1 and data3, synced status
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"1E.C3.EA\" group=\"3\" recordControl=\"162\" data1=\"255\" data2=\"28\" data3=\"4\" syncstatus=\"synced\" sceneid=\"2\" "));
        // Propagate physical to logical
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"1E.C3.EA\" group=\"3\" recordControl=\"162\" data1=\"150\" data2=\"28\" data3=\"5\" syncstatus=\"synced\" sceneid=\"0\""));
        // Mark synced
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"1E.C3.EA\" group=\"3\" recordControl=\"162\" data1=\"150\" data2=\"28\" data3=\"5\" syncstatus=\"synced\" sceneid=\"0\" "));
        // No change to physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"1E.C3.EA\" group=\"3\" recordControl=\"162\" data1=\"150\" data2=\"28\" data3=\"5\""));

        // 3
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.F5.C7\" group=\"4\" recordControl=\"226\" data1=\"0\" data2=\"0\" data3=\"0\""));
        // Partial match of physical record, different data1 and data2, changed status
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"22.F5.C7\" group=\"4\" recordControl=\"226\" data1=\"1\" data2=\"32\" data3=\"65\" syncstatus=\"changed\" sceneid=\"3\""));
        // LogicalDatabase version should be in logical database after read, still marked changed
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.F5.C7\" group=\"4\" recordControl=\"226\" data1=\"1\" data2=\"32\" data3=\"65\" syncstatus=\"changed\" sceneid=\"3\""));
        // Mark sync
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.F5.C7\" group=\"4\" recordControl=\"226\" data1=\"1\" data2=\"32\" data3=\"65\" syncstatus=\"synced\" sceneid=\"3\""));
        // 3 - Propagate data1 and data2 to physical
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.F5.C7\" group=\"4\" recordControl=\"226\" data1=\"1\" data2=\"32\" data3=\"65\""));

        // 4
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.FD.91\" group=\"4\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"4\""));
        // Full match of physical record, deleted (not in use, changed) in logical
        logicalDatabase.Add(new AllLinkRecord("address=\"22.FD.91\" group=\"4\" recordControl=\"98\" data1=\"3\" data2=\"28\" data3=\"4\" syncstatus=\"changed\" sceneid=\"4\""));
        // Should stay in logical database after read, still marked not in use and changed
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord("address=\"22.FD.91\" group=\"4\" recordControl=\"98\" data1=\"3\" data2=\"28\" data3=\"4\" syncstatus=\"changed\" sceneid=\"4\""));
        // Should be removed from the logical database after write
        //
        // And from the physical database
        //

        // 5
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.F3.E4\" group=\"5\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\""));
        // Full match of physical record, marked changed in LogicalDatabase
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"22.F3.E4\" group=\"5\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" syncstatus=\"changed\" sceneid=\"5\""));
        // Should be in logical database after read, marked synced
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.F3.E4\" group=\"5\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" syncstatus=\"synced\" sceneid=\"5\""));
        // No change
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.F3.E4\" group=\"5\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\" syncstatus=\"synced\" sceneid=\"5\""));
        // Keep as is since it's a full match
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.F3.E4\" group=\"5\" recordControl=\"162\" data1=\"0\" data2=\"28\" data3=\"8\""));

        // 6
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"21.AA.D0\" group=\"2\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"2\""));
        // Record inexistant in logical database
        //
        // Creates logical record that matches physical record, mark synced since record is in physical database
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"21.AA.D0\" group=\"2\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"2\" syncstatus=\"synced\" sceneid=\"0\""));
        // No change after write
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"21.AA.D0\" group=\"2\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"2\" syncstatus=\"synced\" sceneid=\"0\""));
        // No change
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"21.AA.D0\" group=\"2\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"2\""));

        // 7
        //
        // Not a match of any record in physical database, marked changed
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"23.C1.8A\" group=\"6\" recordControl=\"162\" data1=\"140\" data2=\"28\" data3=\"6\" syncstatus=\"changed\" sceneid=\"6\""));
        // Should stay in the logical database after read, marked changed
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"23.C1.8A\" group=\"6\" recordControl=\"162\" data1=\"140\" data2=\"28\" data3=\"6\" syncstatus=\"changed\" sceneid=\"6\""));
        // Mark synced after write
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"23.C1.8A\" group=\"6\" recordControl=\"162\" data1=\"140\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"6\""));
        // Add record to physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"23.C1.8A\" group=\"6\" recordControl=\"162\" data1=\"140\" data2=\"28\" data3=\"6\""));

        // 8
        //
        // Not a match of any record in physical database, marked synced
        logicalDatabase.Add(new AllLinkRecord(                   "address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"140\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"6\""));
        // Kept in logical database after read and marked change
        // TODO: Should this be removed from the logical database and not written to physical database instead?
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"140\" data2=\"28\" data3=\"6\" syncstatus=\"changed\" sceneid=\"6\""));
        // Mark synced after write
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"140\" data2=\"28\" data3=\"6\" syncstatus=\"synced\" sceneid=\"6\""));
        // Add record to physical database
        // TODO: consider not writing this to match regular devices
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.F3.E5\" group=\"6\" recordControl=\"162\" data1=\"140\" data2=\"28\" data3=\"6\""));

        // 9
        physicalDatabase.Add(new AllLinkRecord(                  "address=\"22.E5.A0\" group=\"0\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"0\""));
        // No match in the logical database
        //
        // Add to the logical database after read, mark synced since it is in the physical database
        expectedLogicalDatabaseAfterRead.Add(new AllLinkRecord(  "address=\"22.E5.A0\" group=\"0\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"0\" syncstatus=\"synced\" sceneid=\"0\""));
        // No change after write
        expectedLogicalDatabaseAfterWrite.Add(new AllLinkRecord( "address=\"22.E5.A0\" group=\"0\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"0\" syncstatus=\"synced\" sceneid=\"0\""));
        // No change to physical database
        expectedPhysicalDatabaseAfterWrite.Add(new AllLinkRecord("address=\"22.E5.A0\" group=\"0\" recordControl=\"226\" data1=\"3\" data2=\"28\" data3=\"0\""));

        return (logicalDatabase, physicalDatabase, expectedLogicalDatabaseAfterRead, expectedLogicalDatabaseAfterWrite, expectedPhysicalDatabaseAfterWrite);
    }

    [TestMethod]
    public async Task TestIMAllLinkDatabase_ReadDatabase()
    {
        (var logicalDatabase, var physicalDatabase, var expectedDatabase, var _, var _) = BuildIMDatabases();

        // This changes logicalDatabase, which we will check against expectedDatabase
        var house = new House()
            .WithGateway()
            .WithMockPhysicalDevice(new MockPhysicalIM(InsteonID.Null, physicalDatabase));
        var imDriver = new IMDriver(house, InsteonID.Null);
        await imDriver.TryReadAllLinkDatabaseAsync(logicalDatabase);

        CompareUnorederedDatabases(logicalDatabase, expectedDatabase);
    }

    [TestMethod]
    public async Task TestIMAllLinkDatabase_ReadDatabase_UnknownStatuses()
    {
        (var logicalDatabase, var physicalDatabase, var expectedDatabase, var _, var _) = BuildIMDatabases();

        ReplaceChangedByUnknown(logicalDatabase);

        // This changes logicalDatabase, which we will check against expectedDatabase
        var house = new House()
            .WithGateway()
            .WithMockPhysicalDevice(new MockPhysicalIM(InsteonID.Null, physicalDatabase));
        var imDriver = new IMDriver(house, InsteonID.Null);
        await imDriver.TryReadAllLinkDatabaseAsync(logicalDatabase);

        CompareUnorederedDatabases(logicalDatabase, expectedDatabase);
    }

    [TestMethod]
    public async Task TestIMAllLinkDatabase_WriteDatabase()
    {
        (var logicalDatabase, var physicalDatabase, var _, var expectedLogicalDatabase, var expectedPhysicalDatabase) = BuildIMDatabases();

        // This changes both logicalDatabase and physicalDatabase, which we will check against expected
        var house = new House()
            .WithGateway()
            .WithMockPhysicalDevice(new MockPhysicalIM(InsteonID.Null, physicalDatabase));
        var imDriver = new IMDriver(house, InsteonID.Null);
        await imDriver.TryWriteAllLinkDatabaseAsync(logicalDatabase, forceRead: true);

        CompareUnorederedDatabases(logicalDatabase, expectedLogicalDatabase);
        CompareUnorederedDatabases(physicalDatabase, expectedPhysicalDatabase, header: "Physical", ignoreSyncStatus: true, ignoreSceneId: true);
    }

    [TestMethod]
    public async Task TestIMAllLinkDatabase_WriteDatabase_UnknownStatuses()
    {
        (var logicalDatabase, var physicalDatabase, var _, var expectedLogicalDatabase, var expectedPhysicalDatabase) = BuildIMDatabases();

        ReplaceChangedByUnknown(logicalDatabase);

        // This changes both logicalDatabase and physicalDatabase, which we will check against expected
        var house = new House()
            .WithGateway()
            .WithMockPhysicalDevice(new MockPhysicalIM(InsteonID.Null, physicalDatabase));
        var imDriver = new IMDriver(house, InsteonID.Null);
        await imDriver.TryWriteAllLinkDatabaseAsync(logicalDatabase, forceRead: true);

        CompareUnorederedDatabases(logicalDatabase, expectedLogicalDatabase);
        CompareUnorederedDatabases(physicalDatabase, expectedPhysicalDatabase, header: "Physical", ignoreSyncStatus: true, ignoreSceneId: true);
    }

    // Helper to compare two unordered databases (e.g., IM databases)
    private void CompareUnorederedDatabases(AllLinkDatabase database, AllLinkDatabase expectedDatabase, string header = "Logical", bool ignoreSyncStatus = false, bool ignoreSceneId = false)
    {
        // Check that given database database has the proper number of records
        if (expectedDatabase.Count != database.Count)
        {
            DumpDatabase(expectedDatabase, $"{header} ref");
            DumpDatabase(database, $"{header} test");
            Assert.IsTrue(false, $"{header} test database has {database.Count} record, should have {expectedDatabase.Count}");
        }

        // Check given database against expected database
        foreach (var expectedRecord in expectedDatabase)
        {
            if (!database.TryGetEntry(expectedRecord, null, out AllLinkRecord? matchingRecord) ||
                (!ignoreSyncStatus && expectedRecord.SyncStatus != matchingRecord.SyncStatus) ||
                (!ignoreSceneId && expectedRecord.SceneId != matchingRecord.SceneId))
            {
                DumpDatabase(expectedDatabase, $"{header} ref");
                DumpDatabase(database, $"{header} test");
                Assert.IsTrue(false, $"{header} test is missing ref record: {expectedRecord.GetLogOutput(-1, null, showNotInUseRecord: true, showSyncStatus: !ignoreSyncStatus, showSceneId: !ignoreSceneId)}");
            }
        }

        // and vice-versa
        foreach (var record in database)
        {
            if (!database.TryGetEntry(record, null, out AllLinkRecord? matchingRecord) ||
                (!ignoreSyncStatus && record.SyncStatus != matchingRecord.SyncStatus) ||
                (!ignoreSceneId && record.SceneId != matchingRecord.SceneId))
            {
                DumpDatabase(expectedDatabase, $"{header} ref");
                DumpDatabase(database, $"{header} test");
                Assert.IsTrue(false, $"{header} test record is not in ref: {record.GetLogOutput(-1, null, showNotInUseRecord: true, showSyncStatus: !ignoreSyncStatus, showSceneId: !ignoreSceneId)}");
            }
        }
    }

    private void ReplaceChangedByUnknown(AllLinkDatabase database)
    {
        for (var i = 0; i < database.Count; i++)
        {
            if (database[i].SyncStatus == SyncStatus.Changed)
            {
                database.UpdateRecordSyncStatus(i, SyncStatus.Unknown);
            }
        }
    }

    private void DumpDatabase(AllLinkDatabase database, string header = "Logical")
    {
        for (int i = 0; i < database.Count; i++)
        {
            TestContext?.WriteLine(database[i].GetLogOutput(i, $"{header}:", showNotInUseRecord: true, showSyncStatus: true, showSceneId: true));
        }
        TestContext? .WriteLine("");
    }
}
