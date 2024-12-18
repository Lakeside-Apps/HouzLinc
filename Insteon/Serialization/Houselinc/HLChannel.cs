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
using Common;
using Insteon.Model;

namespace Insteon.Serialization.Houselinc;

[XmlType("channel")]
public sealed class HLChannel
{
    public HLChannel()
    {
    }

    public HLChannel(Channel channel)
    {
        Id = channel.Id;
        Name = channel.Name != null ? channel.Name.NullIfEmpty() : null;
        LastStatus = channel.PropertiesSyncStatus;
        if (channel.Device.IsKeypadLinc)
        {
            FollowMask = channel.FollowMask;
            FollowMaskLastUpdate = channel.FollowMaskLastUpdate;

            FollowOffMask = channel.FollowOffMask;
            FollowOffMaskLastUpdate = channel.FollowOffMaskLastUpdate;

            ToggleMode = channel.ToggleMode;
            ToggleModeLastUpdate = channel.ToggleModeLastUpdate;

            LEDOn = channel.LEDOn ? 1 : 0;
            LEDOnLastUpdate = channel.LEDOnLastUpdate;

            OnLevel = channel.OnLevel;
            OnLevelLastUpdate = channel.OnLevelLastUpdate;

            RampRate = channel.RampRate;
            RampRateLastUpdate = channel.RampRateLastUpdate;
        }
    }

    public Channel BuildModel(Device device)
    {
        var channel = new Channel(device.Channels, fromDeserialization: true)
        {
            Id = Id,
            Name = Name,
            PropertiesSyncStatus = LastStatus,
        };

        if (device.IsKeypadLinc)
        {
            if (FollowMaskSerialized != null && FollowMaskSerialized.Value != null)
            {
                channel.FollowMask = FollowMask;
                channel.FollowMaskLastUpdate = FollowMaskLastUpdate;
            }
            if (FollowOffMaskSerialized != null && FollowOffMaskSerialized.Value != null)
            {
                channel.FollowOffMask = FollowOffMask;
                channel.FollowOffMaskLastUpdate = FollowOffMaskLastUpdate;
            }
            if (ToggleModeSerialized != null && ToggleModeSerialized.Value != null)
            {
                channel.ToggleMode = ToggleMode;
                channel.ToggleModeLastUpdate = ToggleModeLastUpdate;
            }
            if (LEDOnSerialized != null && LEDOnSerialized.Value != null)
            {
                channel.LEDOn = LEDOn != 0;
                channel.LEDOnLastUpdate = LEDOnLastUpdate;
            }
            else
            {
                // Default for LEDOn is true
                channel.LEDOn = true;
            }
            if (OnLevelSerialized != null && OnLevelSerialized.Value != null)
            {
                channel.OnLevel = OnLevel;
                channel.OnLevelLastUpdate = OnLevelLastUpdate;
            }
            if (RampRateSerialized != null && RampRateSerialized.Value != null)
            {
                channel.RampRate = RampRate;
                channel.RampRateLastUpdate = RampRateLastUpdate;
            }
        }
        return channel;
    }

    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("name")]
    public string? Name { get; set; }

    [XmlAttribute("lastStatus")]
    public string? LastStatusSerialized
    {
        get => SyncStatusHelper.Serialize(LastStatus);
        set { LastStatus = value != null ? SyncStatusHelper.Deserialize(value) : SyncStatus.Unknown; }
    }
    [XmlIgnore]
    public SyncStatus LastStatus = SyncStatus.Unknown;

    [XmlElement("onMask")]
    public HLChannelProp? FollowMaskSerialized { get; set; }
    [XmlIgnore]
    public int FollowMask
    {
        get => (FollowMaskSerialized != null && FollowMaskSerialized.Value != null) ? FollowMaskSerialized.Value.Value : 0;
        set
        {
            if (value != 0 || FollowMaskLastUpdate != default)
            {
                FollowMaskSerialized ??= new HLChannelProp();
                FollowMaskSerialized.Value ??= new HLChannelPropValue();
                FollowMaskSerialized.Value.Value = value;
            }
        }
    }
    [XmlIgnore]
    private DateTime FollowMaskLastUpdate
    {
        get => (FollowMaskSerialized != null && FollowMaskSerialized.Value != null) ? FollowMaskSerialized.Value.LastUpdate : default;
        set
        {
            if (value != default)
            {
                FollowMaskSerialized ??= new HLChannelProp();
                FollowMaskSerialized.Value ??= new HLChannelPropValue();
                FollowMaskSerialized.Value.LastUpdate = value;
            }
        }
    }

    [XmlElement("offMask")]
    public HLChannelProp? FollowOffMaskSerialized { get; set; }
    [XmlIgnore]
    public int FollowOffMask
    {
        get => (FollowOffMaskSerialized != null && FollowOffMaskSerialized.Value != null) ? FollowOffMaskSerialized.Value.Value : 0;
        set
        {
            if (value != 0 || FollowOffMaskLastUpdate != default)
            {
                FollowOffMaskSerialized ??= new HLChannelProp();
                FollowOffMaskSerialized.Value ??= new HLChannelPropValue();
                FollowOffMaskSerialized.Value.Value = value;
            }
        }
    }
    [XmlIgnore]
    private DateTime FollowOffMaskLastUpdate
    {
        get => (FollowOffMaskSerialized != null && FollowOffMaskSerialized.Value != null) ? FollowOffMaskSerialized.Value.LastUpdate : default;
        set
        {
            if (value != default)
            {
                FollowOffMaskSerialized ??= new HLChannelProp();
                FollowOffMaskSerialized.Value ??= new HLChannelPropValue();
                FollowOffMaskSerialized.Value.LastUpdate = value;
            }
        }
    }

    [XmlElement("toggleMode")]
    public HLChannelToggleMode? ToggleModeSerialized { get; set; }
    [XmlIgnore]
    public ToggleMode ToggleMode
    {
        get => (ToggleModeSerialized != null && ToggleModeSerialized.Value != null) ? ToggleModeSerialized.Value.Value : 0;
        set
        {
            // Default value is Toggle
            if (value != ToggleMode.Toggle || ToggleModeLastUpdate != default)
            {
                ToggleModeSerialized ??= new HLChannelToggleMode();
                ToggleModeSerialized.Value ??= new HLChannelToggleModeValue();
                ToggleModeSerialized.Value.Value = value;
            }
        }
    }

    [XmlIgnore]
    private DateTime ToggleModeLastUpdate
    {
        get => (ToggleModeSerialized != null) && ToggleModeSerialized.Value != null ? ToggleModeSerialized.Value.LastUpdate : default;
        set 
        {
            if (value != default)
            {
                ToggleModeSerialized ??= new HLChannelToggleMode();
                ToggleModeSerialized.Value ??= new HLChannelToggleModeValue();
                ToggleModeSerialized.Value.LastUpdate = value;
            }
        }
    }

    [XmlElement("LEDon")]
    public HLChannelProp? LEDOnSerialized { get; set; }
    [XmlIgnore]
    public int LEDOn
    {
        // Default value is 1
        get => (LEDOnSerialized != null && LEDOnSerialized.Value != null) ? LEDOnSerialized.Value.Value : 1;
        set
        {
            // Default value is 1
            if (value != 1 || LEDOnLastUpdate != default)
            {
                LEDOnSerialized ??= new HLChannelProp();
                LEDOnSerialized.Value ??= new HLChannelPropValue();
                LEDOnSerialized.Value.Value = value;
            }
        }
    }

    [XmlIgnore]
    private DateTime LEDOnLastUpdate
    {
        get => (LEDOnSerialized != null && LEDOnSerialized.Value != null) ? LEDOnSerialized.Value.LastUpdate : default;
        set
        {
            if (value != default)
            {
                LEDOnSerialized ??= new HLChannelProp();
                LEDOnSerialized.Value ??= new HLChannelPropValue();
                LEDOnSerialized.Value.Value = 1;    // default value is 1
                LEDOnSerialized.Value.LastUpdate = value;
            }
        }
    }

    [XmlElement("onLevel")]
    public HLChannelProp? OnLevelSerialized { get; set; }
    [XmlIgnore]
    public int OnLevel
    {
        get => (OnLevelSerialized != null && OnLevelSerialized.Value != null) ? OnLevelSerialized.Value.Value : 0;
        set
        {
            if (value != 0 || OnLevelLastUpdate != default)
            {
                OnLevelSerialized ??= new HLChannelProp();
                OnLevelSerialized.Value ??= new HLChannelPropValue();
                OnLevelSerialized.Value.Value = value;
            }
        }
    }
    [XmlIgnore]
    private DateTime OnLevelLastUpdate
    {
        get => (OnLevelSerialized != null && OnLevelSerialized.Value != null) ? OnLevelSerialized.Value.LastUpdate : default;
        set
        {
            if (value != default)
            {
                OnLevelSerialized ??= new HLChannelProp();
                OnLevelSerialized.Value ??= new HLChannelPropValue();
                OnLevelSerialized.Value.LastUpdate = value;
            }
        }
    }

    [XmlElement("rampRate")]
    public HLChannelProp? RampRateSerialized { get; set; }
    [XmlIgnore]
    public int RampRate
    {
        get => (RampRateSerialized != null && RampRateSerialized.Value != null) ? RampRateSerialized.Value.Value : 0;
        set
        {
            if (value != 0 || RampRateLastUpdate != default)
            {
                RampRateSerialized ??= new HLChannelProp();
                RampRateSerialized.Value ??= new HLChannelPropValue();
                RampRateSerialized.Value.Value = value;
            }
        }
    }
    [XmlIgnore]
    private DateTime RampRateLastUpdate
    {
        get => (RampRateSerialized != null && RampRateSerialized.Value != null) ? RampRateSerialized.Value.LastUpdate : default;
        set
        {
            if (value != default)
            {
                RampRateSerialized ??= new HLChannelProp();
                RampRateSerialized.Value ??= new HLChannelPropValue();
                RampRateSerialized.Value.LastUpdate = value;
            }
        }
    }
}

[XmlType]
public sealed class HLChannelProp
{
    [XmlElement("device")]
    public HLChannelPropValue? Value { get; set; }
}

[XmlType]
public sealed class HLChannelPropValue
{
    [XmlAttribute("value")]
    public int Value { get; set; }

    [XmlAttribute("lastUpdate")]
    public string LastUpdateSerialize
    {
        get => LastUpdate.ToString("M/d/yyyy h:mm:ss tt");
        set { LastUpdate = DateTime.Parse(value); }
    }

    [XmlIgnore]
    public DateTime LastUpdate { get; set; }
}

[XmlType]
public sealed class HLChannelToggleMode
{
    [XmlElement("device")]
    public HLChannelToggleModeValue? Value { get; set; }
}

[XmlType]
public sealed class HLChannelToggleModeValue
{
    [XmlAttribute("value")]
    public string ValueSerialized
    {
        set
        {
            if (value.Equals("Off", StringComparison.OrdinalIgnoreCase))
            {
                Value = ToggleMode.Off;
            }
            if (value.Equals("On", StringComparison.OrdinalIgnoreCase))
            {
                Value = ToggleMode.On;
            }
            if (value.Equals("Toggle", StringComparison.OrdinalIgnoreCase))
            {
                Value = ToggleMode.Toggle;
            }
        }

        get
        {
            switch (Value)
            {
                default:
                    return "";
                case ToggleMode.Off:
                    return "Off";
                case ToggleMode.On:
                    return "On";
                case ToggleMode.Toggle:
                    return "Toggle";
            }
        }
    }

    [XmlIgnore]
    public ToggleMode Value { get; set; }

    [XmlAttribute("lastUpdate")]
    public string LastUpdateSerialized
    {
        get => LastUpdate.ToString("M/d/yyyy h:mm:ss tt");
        set { LastUpdate = DateTime.Parse(value); }
    }

    [XmlIgnore]
    public DateTime LastUpdate { get; set; }
}
