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

using System.ComponentModel;
using System.Xml.Serialization;
using Common;
using Insteon.Model;
using static Insteon.Model.AllLinkRecord;

namespace Insteon.Serialization.Houselinc;

/// <summary>
/// The link database for a device, as serialized in the HouseLinc XML
/// This is only for serialization: this class is backed by an AllLinkDatabase, 
/// that we use instead in the rest of the code
/// </summary>
[XmlType("database")]
public sealed class HLLinkDatabase
{
#pragma warning disable CS8618
    // The default constructor is only here to satisfy the XML deserializer
    // which then initialize the object by setting each XML property
    public HLLinkDatabase() { }
#pragma warning restore CS8618

    public HLLinkDatabase(AllLinkDatabase allLinkDatabase)
    {
        LinkRecords = new HLLinkRecords(allLinkDatabase);
        Sequence = allLinkDatabase.Sequence;
        LastUpdate = allLinkDatabase.LastUpdate;
        LastStatus = allLinkDatabase.LastStatus;
        Revision = allLinkDatabase.Revision;
        NextRecordToRead = allLinkDatabase.NextRecordToRead;
        HLReadResumeData = allLinkDatabase.ReadResumeData != null ? new HLReadResumeData(allLinkDatabase.ReadResumeData) : null;
    }

    public AllLinkDatabase BuildModel(House house, Device device)
    {
        var allLinkDatabase = LinkRecords.BuildModel(house, this, device);
        return allLinkDatabase;
    }

    [XmlAttribute("sequence")]
    public int Sequence;

    [XmlAttribute("lastUpdate")]
    public string LastUpdateSerialize
    {
        get => LastUpdate.ToString("M/d/yyyy h:mm:ss tt");
        set => LastUpdate = DateTime.Parse(value);
    }

    [XmlIgnore]
    public DateTime LastUpdate { get; set; }

    [XmlAttribute("lastStatus")]
    public string? LastStatusSerialize
    {
        get => SyncStatusHelper.Serialize(LastStatus);
        set => LastStatus = SyncStatusHelper.Deserialize(value);
    }
    [XmlIgnore]
    public SyncStatus LastStatus = SyncStatus.Unknown;

    // Revision number of the physical device database (DBDelta in INSTEON docs)
    [XmlAttribute("revision")]
    public int Revision { get; set; }

    // next record to read index if revisions match
    [XmlAttribute("nextRecordToRead")]
    [DefaultValue(-1)]
    public int NextRecordToRead { get; set; } = -1;

    // Not used, for round trip from/to houselinc.xml
    [XmlElement("readResumeData")]
    public HLReadResumeData? HLReadResumeData { get; set; }

    [XmlElement("record")]
    public HLLinkRecords LinkRecords { get; set; }
}

/// <summary>
/// Sub-element of "database" in the houselinc XML
/// Not much use but keeping it for roundtriping just in case
/// </summary>
[XmlType("readResumeData")]
public sealed class HLReadResumeData
{
    public HLReadResumeData() { }

    public HLReadResumeData(AllLinkDatabase.ReadResumeDataType readResumeData)
    {
        if (readResumeData != null)
        {
            TimeStamp = readResumeData.TimeStamp;
            Sequence = readResumeData.Sequence;
            LastSuccessIndex = readResumeData.LastSuccessIndex;
        }
    }

    public AllLinkDatabase.ReadResumeDataType BuildModel()
    {
        var readResumeData = new AllLinkDatabase.ReadResumeDataType()
        {
            TimeStamp = TimeStamp ?? string.Empty,
            Sequence = Sequence,
            LastSuccessIndex = LastSuccessIndex,
        };
        return readResumeData;
    }

    [XmlAttribute("timeStamp")]
    public string? TimeStamp { get; set; }

    [XmlAttribute("sequence")]
    public int Sequence { get; set; }

    [XmlAttribute("lastSuccessIndex")]
    public int LastSuccessIndex { get; set; }
}

/// <summary>
/// A collection of LinkRecords, backed by an AllLinkDatabase
/// </summary>
[XmlType]
public sealed class HLLinkRecords : List<HLLinkRecord>
{
    public HLLinkRecords()
    {
    }

    public HLLinkRecords(AllLinkDatabase allLinkDatabase)
    {
        int seq = 0;
        foreach (var record in allLinkDatabase)
        {
            Add(new HLLinkRecord(seq++, record));
        }
    }

    public AllLinkDatabase BuildModel(House house, HLLinkDatabase hlLinkDatabase, Device device)
    {
        var allLinkDatabase = new AllLinkDatabase()
        {
            Sequence = hlLinkDatabase.Sequence,
            Revision = hlLinkDatabase.Revision,
            NextRecordToRead = hlLinkDatabase.NextRecordToRead,
            ReadResumeData = hlLinkDatabase.HLReadResumeData?.BuildModel(),
            // Keep these two last to restore proper status and last update time
            LastStatus = hlLinkDatabase.LastStatus,
            LastUpdate = hlLinkDatabase.LastUpdate,
        };

        foreach (var hlRecord in this)
        {
            allLinkDatabase.Add(hlRecord.BuildModel(house));
        }
        return allLinkDatabase;
    }
}

/// <summary>
/// A link, as serialized in the HouseLinc XML
/// </summary>
[XmlType]
public sealed class HLLinkRecord
{
    public HLLinkRecord()
    {
    }

    public HLLinkRecord(int sequence, AllLinkRecord allLinkRecord)
    {
        Sequence = sequence;
        hlAllLinkRecord = new HLAllLinkRecord(allLinkRecord);
    }

    public AllLinkRecord BuildModel(House house)
    {
        if (hlAllLinkRecord != null)
            return hlAllLinkRecord.BuildModel(house);
        else
            return new AllLinkRecord();
    }

    [XmlAttribute("sequence")]
    public int Sequence { get; set; }

    [XmlElement("device")]
    public HLAllLinkRecord DeviceValue
    {
        get
        {
            return hlAllLinkRecord;
        }
        set
        {
            hlAllLinkRecord = value;
        }
    }

    [XmlElement("pending")]
    // Used in houselinc.xml instead of "device"
    // We read it as "device" and don't persist it
    public HLAllLinkRecord? PendingValue
    {
        get
        {
            return null;
        }
        set
        {
            if (value != null) 
                hlAllLinkRecord = value;
        }
    }

    private HLAllLinkRecord hlAllLinkRecord { get; set; } = null!;
}

/// <summary>
/// The content of a link record, as serialized in the HouseLinc XML
/// </summary>
[XmlType]
public sealed class HLAllLinkRecord
{
    public HLAllLinkRecord() { }

    public HLAllLinkRecord(AllLinkRecord allLinkRecord)
    {
        this.Uid = allLinkRecord.Uid;
        this.DestID = allLinkRecord.DestID;
        this.Group = allLinkRecord.Group;
        this.Flags= allLinkRecord.Flags;
        this.Data1 = allLinkRecord.Data1;
        this.Data2 = allLinkRecord.Data2;
        this.Data3 = allLinkRecord.Data3;
        this.SceneId= allLinkRecord.SceneId;
        this.SyncStatus = allLinkRecord.SyncStatus;
    }

    public AllLinkRecord BuildModel(House house)
    {
        // If this record does not have a uid, we create one and we request that the model 
        // be saved back to disk/service immediately after loading, to ensure that all
        // instances of the app get the same uid for the same record
        if (Uid == 0)
            house.RequestSaveAfterLoad = true;

        var allLinkRecord = new AllLinkRecord
        {
            DestID = this.DestID,
            Group = this.Group,
            Flags = this.Flags,
            Data1 = this.Data1,
            Data2 = this.Data2,
            Data3 = this.Data3,
            SceneId = this.SceneId,
            SyncStatus = this.SyncStatus,
            // Uid should be last as setting other properties (except SyncStatus) will reset it
            Uid = (this.Uid != 0) ? this.Uid : (uint)System.Guid.NewGuid().GetHashCode(),
        };

        return allLinkRecord;
    }

    [XmlAttribute(AttributeName = "address")]
    public string DestIDSerialized
    {
        get => DestID.ToString();
        set
        {
            if (value != "")
            {
                DestID = new InsteonID(value);
            }
        }
    }

    [XmlIgnore]
    public InsteonID DestID { get; set; } = InsteonID.Null;

    [XmlAttribute("group")]
    public byte Group { get; set; }

    [XmlAttribute("recordControl")]
    public byte RecordControl { get { return (byte)Flags; } set { Flags = (RecordFlags)value; } }

    [XmlIgnore]
    public RecordFlags Flags { get; private set; }

    [XmlAttribute("data1")]
    public byte Data1 { get; set; }

    [XmlAttribute("data2")]
    public byte Data2 { get; set; }

    [XmlAttribute("data3")]
    public byte Data3 { get; set; }

    [XmlAttribute("scene")]
    public int SceneId { get; set; }

    [XmlAttribute("syncstatus")]
    public string? SyncStatusSerialize
    {
        get => SyncStatusHelper.SerializeForLink(SyncStatus);
        set => SyncStatus = SyncStatusHelper.DeserializeForLink(value);
    }

    [XmlIgnore]
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Unknown;

    [XmlAttribute("uid")]
    public uint Uid { get; set; }
}

