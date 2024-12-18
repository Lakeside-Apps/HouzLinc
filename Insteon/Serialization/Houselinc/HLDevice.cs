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

using System.Xml.Serialization;
using Insteon.Base;
using Common;
using Insteon.Model;

namespace Insteon.Serialization.Houselinc;

[XmlType("device")]
public sealed class HLDevice
{
#pragma warning disable CS8618
    // The default constructor is only here to satisfy the XML deserializer
    // which then initializes the object by setting each XML property
    public HLDevice()
    {
    }
#pragma warning restore CS8618

    public HLDevice(Device device)
    {
        InsteonID = device.Id;
        CategoryId = device.CategoryId;
        SubCategory = device.SubCategory;
        Revision = device.Revision;
        Status = device.Status;
        Powerline = device.Powerline;
        Radio = device.Radio;
        YakityYak = device.YakityYak;
        HopIfModSyncFails = device.HopIfModSyncFails;
        ProductKey = device.ProductKey;
        Wattage = device.Wattage;
        WebDeviceID = device.WebDeviceID;
        WebGatewayControllerGroup = WebGatewayControllerGroup;
        Driver = device.Driver;
        DisplayName = device.DisplayName;
        if (device.AddedDateTime != default)
        {
            Notes = new HLNotes();
            Notes.Added = device.AddedDateTime.ToString("M/d/yyyy h:mm:ss tt");
        }
        Location = new HLLocation(device);
        customProperties = device.customProperties;
        Channels = new HLChannels(device.Channels);
        LinkDatabase = new HLLinkDatabase(device.AllLinkDatabase);
        PropertiesXMLWrapper = new HLPropertiesXMLWrapper(device);
    }

    public Device BuildModel(House house, Devices devices)
    {
        var device = new Device(devices, InsteonID, fromDeserialization: true)
        {
            CategoryId = CategoryId,
            SubCategory = SubCategory,
            Revision = Revision,
            Status = Status,
            Powerline = Powerline,
            Radio = Radio,
            YakityYak = YakityYak,
            HopIfModSyncFails = HopIfModSyncFails,
            ProductKey = ProductKey,
            Wattage = Wattage,
            WebDeviceID = WebDeviceID,
            WebGatewayControllerGroup = WebGatewayControllerGroup,
            Driver = Driver,
            DisplayName = DisplayName,
            AddedDateTime = DateTime.TryParse(Notes?.Added, out DateTime parsedDate) ? parsedDate : default,
            Location = Location?.Value,
            Room = Location?.Room,
            customProperties = customProperties,
        };

        PropertiesXMLWrapper?.BuildModel(device);
        device.Channels = Channels?.BuildModel(device) ?? new Channels();
        device.AllLinkDatabase = LinkDatabase?.BuildModel(house, device) ?? new AllLinkDatabase();

        return device;
    }

    // -----------------------------------------------------------------------------------
    // Serilized properties
    // These are properties serialized /deserialized to/from the XML persisted model
    // ------------------------------------------------------------------------------------

    [XmlAttribute("insteonID")]
    public string InsteonIDSerialized
    {
        get
        {
            return InsteonID.ToString();
        }

        set
        {
            if (value != null && value != "")
            {
                InsteonID = new InsteonID(value);
            }
        }
    }
    [XmlIgnore]
    public InsteonID InsteonID { get; set; } = InsteonID.Null;

    [XmlAttribute("category")]
    public int CategorySerialized { get => (int)CategoryId; set => CategoryId = (DeviceKind.CategoryId) value; }
    [XmlIgnore]
    public DeviceKind.CategoryId CategoryId { get; set; }

    [XmlAttribute("subcategory")]
    public int SubCategorySerialized { get => SubCategory; set => SubCategory = value; }
    [XmlIgnore]
    public int SubCategory { get; set; }

    [XmlAttribute("revision")]
    public int RevisionSerialized { get => Revision; set => Revision = value; }
    [XmlIgnore]
    public int Revision { get; set; }

    /// <summary>
    /// Device status
    /// </summary>
    [XmlAttribute("status")]
    public string? StatusSerialized
    {
        get
        {
            switch (Status)
            {
                default:
                    return null;
                case Device.ConnectionStatus.Connected:
                    return "connected";
                case Device.ConnectionStatus.Disconnected:
                    return "disconnected";
                case Device.ConnectionStatus.GatewayError:
                    return "gatewayError";
            }
        }
        set
        {
            switch (value)
            {
                default:
                    Status = Device.ConnectionStatus.Unknown;
                    break;
                case "connected":
                    Status = Device.ConnectionStatus.Connected;
                    break;
                case "disconnected":
                    Status = Device.ConnectionStatus.Disconnected;
                    break;
                case "gatewayError":
                    Status = Device.ConnectionStatus.GatewayError;
                    break;
            }
        }
    }
    [XmlIgnore]
    public Device.ConnectionStatus Status = Device.ConnectionStatus.Unknown;

    [XmlAttribute("powerline")]
    public int PowerlineSerialized { get => (Powerline ? 1 : 0); set => Powerline = (value != 0); }
    [XmlIgnore]
    public bool Powerline { get; set; }

    [XmlAttribute("radio")]
    public int RadioSerialized { get => (Radio ? 1 : 0); set => Radio = (value != 0); }
    [XmlIgnore]
    public bool Radio { get; set; }

    [XmlAttribute("yakityYak")]
    public string? YakityYak { get; set; }

    [XmlAttribute("hopIfModSyncFails")]
    public int HopIfModSyncFailsSerialized { get => (HopIfModSyncFails ? 1 : 0); set => HopIfModSyncFails = (value != 0); }
    [XmlIgnore]
    public bool HopIfModSyncFails { get; set; }

    // TODO: implement product key serialization/deserialization
    [XmlAttribute("ipk")]
    public string ProductKeySerialized { get => "00.00.00"; set => ProductKey = 0; }
    [XmlIgnore]
    public int ProductKey { get; set; }

    [XmlAttribute("wattage")]
    public int Wattage { get; set; }

    [XmlAttribute("webDeviceID")]
    public int WebDeviceID { get; set; }

    [XmlAttribute("webGatewayControllerGroup")]
    public int WebGatewayControllerGroup { get; set; }

    [XmlAttribute("driver")]
    public string Driver { get; set; }

    [XmlAttribute("displayName")]
    public string DisplayName { get; set; }

    // Not used, was in Houselinc, always "1"
    [XmlAttribute("active")]
    public int HLActive { get; set; } = 1;

    [XmlElement("notes")]
    public HLNotes? Notes { get; set; }

    [XmlElement("location")]
    public HLLocation Location { get; set; }

    [XmlElement("customProperties")]
    public object? customProperties { get; set; }

    [XmlArray("channels")]
    public HLChannels Channels { get; set; }

    [XmlElement("database")]
    public HLLinkDatabase LinkDatabase { get; set; }

    // XML serialization does not handle attributes on XmlArray types, so we use XMLElement 
    // instead and Properties is not a list of properties but instead contains a list of properties
    [XmlElement("properties")]
    public HLPropertiesXMLWrapper PropertiesXMLWrapper { get; set; }
}
