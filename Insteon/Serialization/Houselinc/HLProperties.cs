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

using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using Common;
using Insteon.Model;

namespace Insteon.Serialization.Houselinc;

[XmlType("properties")]
public sealed class HLPropertiesXMLWrapper
{
#pragma warning disable CS8618
    // The default constructor is only here to satisfy the XML deserializer
    // which then initialize the object by setting each XML property
    public HLPropertiesXMLWrapper() { }
#pragma warning restore CS8618


    public HLPropertiesXMLWrapper(Device device)
    {
        LastStatus = device.PropertiesSyncStatus;
        Properties = new HLProperties(device);
    }

    public void BuildModel(Device device)
    {
        Properties.BuildModel(device);
        device.PropertiesSyncStatus = LastStatus;
    }

    [XmlAttribute("lastStatus"), MaybeNull]
    public string LastStatusSerialized
    {
        get => SyncStatusHelper.Serialize(LastStatus);
        set => LastStatus = SyncStatusHelper.Deserialize(value);
    }
    [XmlIgnore]
    public SyncStatus LastStatus = SyncStatus.Unknown;

    [XmlElement("p")]
    public HLProperties Properties { get; set; }
}

public sealed class HLProperties : List<HLProperty>
{
    public HLProperties() { }

    public HLProperties(Device device)
    {
        foreach (DeviceProperty property in device.PropertyBag)
        {
            Add(new HLProperty(property));
        }
    }

    public void BuildModel(Device device)
    {
        foreach (HLProperty hlProperty in this)
        {
            device.PropertyBag.SetValue(hlProperty.Name, hlProperty.DeviceValue.Value, hlProperty.DeviceValue.LastUpdate);
        }
    }
}

[XmlType("p")]
public sealed class HLProperty
{
    public HLProperty() { }
    public HLProperty(DeviceProperty property)
    {
        Name = property.Name;
        DeviceValue = new HLPropValue(property.Value, property.LastUpdate);
        PendingValue = null;
    }

    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("device")]
    public HLPropValue DeviceValue = new HLPropValue();

    [XmlElement("pending")]
    public HLPropValue? PendingValue;
}

public sealed class HLPropValue
{
    public HLPropValue() { }

    public HLPropValue(Bits bits)
    {
        Value = bits;
    }

    public HLPropValue(Bits bits, DateTime lastUpdate)
    {
        Value = bits;
        LastUpdate = lastUpdate;
    }

    [XmlAttribute("value")]
    public string ValueSerialized
    {
        get => Value.ToString();
        set
        {
            if (value.Substring(0, 2) == "0x" && value.Length == 4)
            {
                Value = new Bits(byte.Parse(value.Substring(2), System.Globalization.NumberStyles.HexNumber));
            }
            else
            {
                Value = new Bits(0);
            }
        }
    }

    [XmlIgnore]
    public Bits Value { get; set; } = 0;

    [XmlAttribute("lastUpdate")]
    public string? LastUpdateSerialized
    {
        get => (LastUpdate != default) ? LastUpdate.ToString("M/d/yyyy h:mm:ss tt") : null;
        set => LastUpdate = value != null ? DateTime.Parse(value) : default;
    }

    [XmlIgnore]
    public DateTime LastUpdate { get; set; } = default;
}
