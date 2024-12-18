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

namespace Insteon.Model;

/// <summary>
///  All-Link record stored in IM or device databases
///
///  Format:
///  ID:          Destination device ID
///  Group:       All-link group number this device ID belongs to
///  RecordFlags: Bit 7: 1 = record in use, 0 = record is available
///               Bit 6: type - 1 = controller link, 0 = responder link
///               Bit 5: Product dependent, seems often 1
///               Bit 4: Product dependent, seems often 0
///               Bit 3: Reserved
///               Bit 2: Reserved
///               Bit 1: Record has been used before, 0 = "high-water mark"
///               Bit 0: Reserved
///
/// Responder Records
///  Data 1    Link-specific data (e.g.On-Level)
///  Data 2    Link-specific data (e.g.Ramp Rates, Setpoints, etc.). 
///            Ramp Rate: 31 = .1s, 28 = .5s, 0 = local responder device value?
///  Data 3    Link-specific data. 
///            - For KPL type devices, responding button number (01-08) on the device the responder record is on
///            - For the IM (hub), always 0
///
/// Controller Records
///  Data 1    Number of retries(Normally set to 03, FF = no retries, 00 = Broadcast for cleanup)
///  Data 2    Listed as Ignored, but seems set to ramp rate, e.g., 28
///  Data 3    Listed as 00 for switchlinc type devices and 01-08 for KPL type devices (matches group)
/// </summary>
public sealed class AllLinkRecord
{
    internal class InvalidAllLinkRecordException : Exception
    {
        internal InvalidAllLinkRecordException(string message) : base(message)
        {
            Logger.Log.Debug(message);
        }

        internal InvalidAllLinkRecordException(string message, Exception e) : base(message, e)
        {
            Logger.Log.Debug(message);
        }
    }

    public AllLinkRecord()
    {
    }

    internal AllLinkRecord(InsteonID destID, bool isController, byte group)
    {
        DestID = destID;
        Group = group;
        IsController = isController;
    }

    public AllLinkRecord(AllLinkRecord record)
    {
        Address = record.Address;
        DestID = record.DestID;
        Group = record.Group;
        Flags = record.Flags;
        Data1 = record.Data1;
        Data2 = record.Data2;
        Data3 = record.Data3;
        SceneId = record.SceneId;
        SyncStatus = record.syncStatus;
        // Uid should be last as setting other properties (except SyncStatus) will reset it
        Uid = record.Uid;
    }

    internal AllLinkRecord(HexString hexString)
    {
        Debug.Assert(hexString.ByteCount >= MessageLength, "HexString passed to an AllLinkRecordMessage is too short");

        Flags = (RecordFlags)hexString.Byte(1);
        Group = hexString.Byte(2);
        DestID = new InsteonID(hexString.Byte(3), hexString.Byte(4), hexString.Byte(5));
        Data1 = hexString.Byte(6);
        Data2 = hexString.Byte(7);
        Data3 = hexString.Byte(8);
    }

    internal AllLinkRecord(InsteonExtendedMessage message)
    {
        if (!IsChecksumValid(message))
        {
            throw new InvalidAllLinkRecordException("AllLinkRecord extended message checksum invalid");
        }

        // Data byte 2 of the message should be 0x01
        if (message.DataByte(2) != 0x01)
        {
            throw new InvalidAllLinkRecordException("AllLinkRecord extended message data 2 should be 0x01");
        }

        // Parse address and check for validity
        // First record is at 0xFFF and records extend downward in memory
        Address = (ushort)(message.DataByte(3) << 8 | message.DataByte(4));
        if ((0xFFF - Address) % RecordByteLength != 0)
        {
            throw new InvalidAllLinkRecordException("AllLinkRecord address invalid");
        }

        // Parse the rest of the message
        Flags = (RecordFlags)message.DataByte(6);
        Group = message.DataByte(7);
        DestID = message.DataBytesAsInsteonID(8);
        Data1 = message.DataByte(11);
        Data2 = message.DataByte(12);
        Data3 = message.DataByte(13);
    }

    /// <summary>
    /// Create a high water mark record to use at end of an all-link database
    /// </summary>
    /// <returns></returns>
    internal static AllLinkRecord CreateHighWaterMark(SyncStatus syncStatus = SyncStatus.Changed)
    {
        return new AllLinkRecord() { Flags = 0, SyncStatus = syncStatus };
    }

    /// <summary>
    /// Create a high water mark record to use at end of an all-link database
    /// </summary>
    /// <returns></returns>
    internal static AllLinkRecord CreateHighWaterMark(uint uid, SyncStatus syncStatus = SyncStatus.Changed)
    {
        return new AllLinkRecord() { Flags = 0, Uid = uid, SyncStatus = syncStatus };
    }

    /// <summary>
    ///  Insteon message representing All-Link records have an checksum that this method tests
    /// </summary>
    /// <param name="message">Insteon extended response message</param>
    /// <returns>true if checksum is valid</returns>
    private static bool IsChecksumValid(InsteonExtendedMessage message)
    {
        // Check that the checksum is valid
        int checksum = message.Command1 + message.Command2;
        for (int i = 1; i < 14; i++)
        {
            checksum += message.DataByte(i);
        }

        checksum = 0xFF - (checksum & 0xFF) + 1 & 0xFF;

        if (checksum != message.DataByte(14))
        {
            return false;
        }

        return true;
    }

    // Byte length of the response message used by the IM to return its All-Link records
    internal static readonly int MessageLength = 8;

    // Records are 8 bytes long in the device database
    internal static int RecordByteLength = 8;

    /// <summary>
    /// Constructor from houselinc style attribute string
    /// address="1E.C3.EA" group="11" recordControl="162" data1="0" data2="28" data3="4" modified="11/28/2014 3:53:23 PM"
    /// </summary>
    /// <param name="attributes"></param>
    internal AllLinkRecord(string attributes)
    {
        string[] delimiter = { "\" " };
        string[] attrArry = attributes.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
        foreach (string attribute in attrArry)
        {
            string[] nameValuePair = attribute.Split('=');
            string name = nameValuePair[0];
            string value = nameValuePair[1].Trim('"');
            switch (name)
            {
                case "address":
                    DestID = new InsteonID(value);
                    break;
                case "group":
                    Group = byte.Parse(value);
                    break;
                case "recordControl":
                    Flags = (RecordFlags)int.Parse(value);
                    break;
                case "data1":
                    Data1 = byte.Parse(value);
                    break;
                case "data2":
                    Data2 = byte.Parse(value);
                    break;
                case "data3":
                    Data3 = byte.Parse(value);
                    break;
                case "syncstatus":
                    SyncStatus = SyncStatusHelper.DeserializeForLink(value);
                    break;
                case "sceneid":
                    SceneId = int.Parse(value);
                    break;
                case "uid":
                    Uid = uint.Parse(value);
                    break;
            }
        }

        DestID ??= new InsteonID();
    }

    // Last used record in the database
    public bool IsLast
    {
        get => IsHighWatermark;
        init { IsHighWatermark = value; }
    }

    public bool IsHighWatermark
    {
        get => (Flags & RecordFlags.UsedBefore) == 0;
        init { if (value) Flags &= ~RecordFlags.UsedBefore; else Flags |= RecordFlags.UsedBefore; }
    }

    public bool IsController
    {
        get => (Flags & RecordFlags.Controller) != 0;
        init { if (value) Flags |= RecordFlags.Controller; else Flags &= ~RecordFlags.Controller; }
    }

    public bool IsResponder
    {
        get => !IsController;
        init { IsController = !value; }
    }

    public bool IsInUse
    {
        get => (Flags & RecordFlags.InUse) != 0;
        init { if (value) Flags |= RecordFlags.InUse; else Flags &= ~RecordFlags.InUse; }
    }

    public enum RecordFlags : byte
    {
        InUse = 0x80,
        Controller = 0x40,
        UsedBefore = 0x02,
        Bit5 = 0x20,
        CompareMask = InUse | Controller | UsedBefore,
    }

    /// <summary>
    /// Assemble a log output for this record
    /// </summary>
    /// <param name="seq">record sequence number, -1 to no show any sequence number</param>
    /// <param name="msg">optional message to prefix record log</param>
    /// <param name="showNotInUseRecord">true to show data of records that are not in use</param>
    /// <returns></returns>
    internal string GetLogOutput(int seq, string? msg = null, bool showNotInUseRecord = false, bool showSyncStatus = false, bool showSceneId = false)
    {
        msg = (msg != null ? msg + " " : "") +
            "Record " + (seq != -1 ? seq.ToString() + ", " : "") +
            (Address != 0 ? "(" + Convert.ToString(Address, 16) + "), " : "");

        if (IsInUse || showNotInUseRecord)
        {
            msg += "ID: " + DestID.ToString() +
            ", Group: " + Group.ToString() +
            ", Flags: " + Flags.ToString() + " (" + (IsController ? "Controller" : "Responder") + ")" +
            ", Data1: " + Data1.ToString() +
            ", Data2: " + Data2.ToString() +
            ", Data3: " + Data3.ToString();
        }
        else
        {
            msg += ", Not in use" + (IsLast ? ", last" : "");
        }

        if (showSyncStatus)
        {
            msg += ", SyncStatus: " + SyncStatus;
        }

        if (showSceneId)
        {
            msg += ", SceneId: " + SceneId;
        }

        return msg;
    }

    /// <summary>
    ///  Log content of a record 
    /// </summary>
    /// <param name="seq">see GetLogOutput</param>
    /// <param name="msg">see GetLogOutput</param>
    /// <param name="showNotInUseRecord">see GetLogOutput</param>
    internal void LogCommandOutput(int seq, string? msg = null, bool showNotInUseRecord = false)
    {
        Logger.Log.CommandOutput(GetLogOutput(seq, msg, showNotInUseRecord));
    }

    internal void LogDebug(int seq, string? msg = null, bool showNotInUseRecord = false)
    {
        Logger.Log.Debug(GetLogOutput(seq, msg, showNotInUseRecord));
    }

    /// <summary>
    /// Parse a string into the flags
    /// </summary>
    /// <param name="s"></param>
    public static RecordFlags ParseFlags(string s)
    {
        return (RecordFlags)byte.Parse(s);
    }

    /// <summary>
    /// Unique identifier for this record.
    /// This uid identifies the record in the database and is used to track changes.
    /// It is:
    /// - compact (uint) and fast to compare,
    /// - immutable for the life of the record,
    /// - auto-generated on first access,
    /// - quasi unique for a given database across all instances of the app (*),
    /// - persisted with the model if the record belongs to a database,
    /// (*) For a database of 500 records, the probability of a collision is 1 in 10^7.
    /// If a record is cloned using the copy constructor, the copy has the same uid.
    /// If the cloned record is modified during object initialization, the uid is reset 
    /// to force a new value to be generated on next access. But Initializing SyncStatus does 
    /// not reset the uid, as it is a status that does not affect the identity of the record).
    /// </summary>
    public uint Uid
    {
        get => (uid != 0) ? uid : uid = (uint)Guid.NewGuid().GetHashCode();
        init => uid = value;
    }
    private uint uid = 0;

    /// <summary>
    /// Produces a hash code for this link record
    /// </summary>
    public override int GetHashCode()
    {
        return DestID.ToInt();
    }

    /// <summary>
    /// Computes equality between this and another record
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as AllLinkRecord, ignoreInUse: false);
    }

    // TODO: consider implementing the equality operators
    //public static bool operator ==(AllLinkRecord? record1, AllLinkRecord? record2)
    //{
    //    if (record1 == null || record2 == null)
    //    {
    //        return false;
    //    }
    //    return record1.Equals(record2);
    //}

    //public static bool operator !=(AllLinkRecord? record1, AllLinkRecord? record2)
    //{
    //    return !(record1 == record2);
    //}

    /// <summary>
    /// Computes equality between this and another record
    /// </summary>
    internal bool Equals(AllLinkRecord? record, bool ignoreInUse)
    {
        if (record != null)
        {
            RecordFlags flagMask = RecordFlags.CompareMask;
            if (ignoreInUse)
                flagMask &= ~RecordFlags.InUse;

            return DestID == record.DestID &&
                    Group == record.Group &&
                    (Flags & flagMask) == (record.Flags & flagMask) &&
                    Data1 == record.Data1 &&
                    Data2 == record.Data2 &&
                    Data3 == record.Data3 &&
                    (SceneId == 0 || record.SceneId == 0 || SceneId == record.SceneId);
        }
        return false;
    }

    /// <summary>
    /// Comparer type checking Destination ID and Group for equality
    /// </summary>
    internal class IdGroupComparer : IEqualityComparer<AllLinkRecord>
    {
        public bool Equals(AllLinkRecord? record1, AllLinkRecord? record2)
        {
            if (record1 == null || record2 == null) return false;
            return record1.DestID == record2.DestID &&
                   record1.Group == record2.Group &&
                   record1.IsInUse == record2.IsInUse;
        }

        public int GetHashCode(AllLinkRecord obj)
        {
            return obj.DestID.ToInt();
        }

        public static IdGroupComparer Instance = new IdGroupComparer();
    }

    /// <summary>
    /// Comparer type checking Destination ID, Group, Type (Controller/Responder) for equality
    /// </summary>
    internal class IdGroupTypeComparer : IEqualityComparer<AllLinkRecord>
    {
        public bool Equals(AllLinkRecord? record1, AllLinkRecord? record2)
        {
            if (record2 == null || record1 == null) return false;
            return record1.DestID == record2.DestID &&
                   record1.Group == record2.Group &&
                   record1.IsController == record2.IsController &&
                   record1.IsInUse == record2.IsInUse;
        }

        public int GetHashCode(AllLinkRecord obj)
        {
            return obj.DestID.ToInt();
        }

        public static IdGroupTypeComparer Instance = new IdGroupTypeComparer();
    }

    /// <summary>
    /// Whether this record is strictly identical to another record
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    internal bool IsIdenticalTo(AllLinkRecord record)
    {
        // We compare the actuall records because the SyncStatus can change without affecting the uid
        // TODO: consider making AllLinkRecord fully immutable, including SyncStatus
        return Uid == record.Uid && Equals(record);
    }

    /// <summary>
    /// Returns the complement link for this link from the specified device 
    /// If a device has a controller link to another device, the complement 
    /// link is the matching responder link from this other device to this device
    /// And vice versa.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    internal AllLinkRecord GetComplementLink(InsteonID id)
    {
        return new AllLinkRecord(id, !IsController, Group);
    }

    /// <summary>
    ///  Persisted, immutable data members
    ///  Members that make up the identity of the record null out Uid
    ///  on initialization to force a new id to be generated.
    /// </summary>
    public ushort Address { get; private init; }

    public InsteonID DestID
    {
        get => destID;
        init
        {
            if (value != destID)
            {
                destID = value;
                Uid = 0;
            }
        }
    }
    private InsteonID destID = InsteonID.Null;

    public byte Group 
    { 
        get => group;
        init
        {
            if (value != group)
            {
                group = value;
                Uid = 0;

            }
        }
    }
    private byte group;

    public RecordFlags Flags
    {
        get => flags;
        init
        {
            if (value != flags)
            {
                flags = value;
                Uid = 0;
            }
        }
    } 
    private RecordFlags flags = RecordFlags.InUse | RecordFlags.UsedBefore | RecordFlags.Bit5;

    public int RecordControl
    {
        get => (int)Flags;
        init => Flags = (RecordFlags)value;
    }

    public byte Data1
    {
        get => data1;
        init
        {
            if (value != data1)
            {
                data1 = value;
                Uid = 0;
            }
        }
    }
    private byte data1;

    public byte Data2
    {
        get => data2;
        init
        {
            if (value != data2)
            {
                data2 = value;
                Uid = 0;
            }
        }
    }
    private byte data2;

    public byte Data3
    {
        get => data3;
        init
        {
            if (value != data3)
            {
                data3 = value;
                Uid = 0;
            }
        }
    }
    private byte data3;

    public int SceneId
    { 
        get => sceneId;
        init
        {
            if (value != sceneId)
            {
                sceneId = value;
                Uid = 0;
            }
        }
    }
    private int sceneId;

    /// <summary>
    ///  SyncStatus describes the status of the record in the database
    ///  with regard to synchronization to the physical devices. Since it
    ///  does not affect the identity of the record, its initializer does
    ///  not null out Uid.
    /// </summary>
    public SyncStatus SyncStatus 
    {
        get => syncStatus;
        init => syncStatus = value; 
    }
    private SyncStatus syncStatus;
}
