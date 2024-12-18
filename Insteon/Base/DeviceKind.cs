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

namespace Insteon.Base;

/// <summary>
/// Insteon device categories and subcategories, device models, and devices
/// </summary>
public static class DeviceKind
{
    class Category
    {
        internal string Name = string.Empty;
        internal string Notes = string.Empty;
        internal DeviceModel[] Models = null!;
    }

    public enum CategoryId : int
    {
        GeneralizedControllers = 0x00,
        DimmableLightingControl = 0x01,
        SwitchedLightingControl = 0x02,
        NetworkBridge = 0x03,
        IrrigationControl = 0x04,
        ClimateControl = 0x05,
        PoolAndSpaControl = 0x06,
        SensorAndAcuators = 0x07,
        HomeEntertainment = 0x08,
        EnergyManagement = 0x09,
        BuiltInApplianceControl = 0x0A,
        Plumbing = 0x0B,
        Communication = 0x0C,
        ComputerControl = 0x0D,
        WindowCoverings = 0x0E,
        AccessControl = 0x0F,
        SecurityHealthSafety = 0x10,
        Surveillance = 0x11,
        Automotive = 0x12,
        PetCare = 0x13,
        Toys = 0x14,
        Timekeeping = 0x15,
        Holiday = 0x16,
        LowVoltageLinc = 0x17,
        Count = 0x18,
    };

    private class DeviceModel
    {
        internal string Name = string.Empty;
        internal string Number = string.Empty;
        internal int SubCategory;
        internal ModelType Type = ModelType.Unknown;
        internal int ChannelCount = 1;
    }

    public enum ModelType
    {
        Unknown,
        Hub,
        PowerLinc,
        SmartLinc,
        SerialLinc,
        SwitchLincRelay,
        SwitchLincDimmer,
        KeypadLincRelay,
        KeypadLincDimmer,
        InlineLincRelay,
        InlineLincDimmer,
        RemoteLinc,
        ControlLinc,
        LampLinc,
        SocketLinc,
        OutletLinc,
        FanLinc,
        DimmableBulb,
        ApplianceLinc,
        TimerLinc,
        LowVoltageLinc,
    }

    // Model icon names - keep in same sequence as ModelType
    internal static string[] ModelIcons =
    {
        "",
        "Hub",
        "PowerLinc",
        "SmartLinc",
        "SerialLinc",
        "SwitchLinc",
        "SwitchLinc",
        "KeypadLinc",
        "KeypadLinc",
        "InlineLinc",
        "InlineLinc",
        "RemoteLinc",
        "ControlLinc",
        "LampLinc",
        "SocketLinc",
        "OutletLinc",
        "FanLinc",
        "DimmableBulb",
        "ApplianceLinc",
        "TimerLinc",
        "LowVoltageLinc",
    };

    public static string GetCategoryName(CategoryId category)
    {
        return Categories[(int)category].Name;
    }

    public static string GetModelName(CategoryId category, int subCategory)
    {
        var model = GetModel(category, subCategory);
        return (model != null) ? model.Name : "";
    }

    public static string GetModelNumber(CategoryId category, int subCategory)
    {
        var model = GetModel(category, subCategory);
        return (model != null) ? model.Number: "";
    }

    public static ModelType GetModelType(CategoryId category, int subCategory)
    {
        var model = GetModel(category, subCategory);
        return (model != null) ? model.Type : ModelType.Unknown;
    }

    public static string GetModelTypeAsString(CategoryId category, int subCategory)
    {
        ModelType modelType = GetModelType(category, subCategory);
        return ModelIcons[(int)modelType];
    }

    private static DeviceModel? GetModel(CategoryId category, int subCategory)
    {
        DeviceModel[] Models = Categories[(int)category].Models;
        foreach (DeviceModel model in Models)
        {
            if (subCategory == model.SubCategory)
            {
                return model;
            }
        }
        return null;
    }

    public static int GetChannelCount(CategoryId category, int subCategory)
    {
        return GetModel(category, subCategory)?.ChannelCount ?? 1;
    }

    private static Category[] Categories = {
        new Category() /* 0x00 */
        {
            Name = "Generalized Controllers", Notes = "ControLinc, RemoteLinc, SignaLinc, etc.",
            Models = new DeviceModel[] 
            {
                new DeviceModel() { SubCategory = 0x04, Number = "2430", Type = ModelType.ControlLinc, Name = "ControLinc" },
                new DeviceModel() { SubCategory = 0x05, Number = "2440", Type = ModelType.RemoteLinc, Name = "RemoteLinc", ChannelCount = 6 },
                new DeviceModel() { SubCategory = 0x06, Number = "2830", Type = ModelType.ControlLinc, Name = "ICON Tabletop Controller" },
                new DeviceModel() { SubCategory = 0x09, Number = "2442", Type = ModelType.Unknown, Name = "SignaLinc RF Signal Enhancer" },
                new DeviceModel() { SubCategory = 0x0B, Number = "2443", Type = ModelType.Unknown, Name = "Access Point (Wireless Phase Coupler)" },
                new DeviceModel() { SubCategory = 0x0C, Number = "12005", Type = ModelType.Unknown, Name = "IES Color Touchscreen" },
                new DeviceModel() { SubCategory = 0x0E, Number = "2440EZ", Type = ModelType.RemoteLinc, Name = "RemoteLinc EZ", ChannelCount = 6  },
                new DeviceModel() { SubCategory = 0x10, Number = "2444A2xx4", Type = ModelType.RemoteLinc, Name = "RemoteLinc 2 Keypad, 4 Scene", ChannelCount = 4  },
                new DeviceModel() { SubCategory = 0x11, Number = "2444A3xx", Type = ModelType.RemoteLinc, Name = "RemoteLinc 2 Switch" },
                new DeviceModel() { SubCategory = 0x12, Number = "2444A2xx8", Type = ModelType.RemoteLinc, Name = "RemoteLinc 2 Keypad, 8 Scene", ChannelCount = 8  },
                new DeviceModel() { SubCategory = 0x13, Number = "2993-222", Type = ModelType.Unknown, Name = "Insteon Diagnostics Keypad" },
                new DeviceModel() { SubCategory = 0x14, Number = "2342-432", Type = ModelType.RemoteLinc, Name = "Insteon Mini Remote - 4 Scene (869 MHz)", ChannelCount = 4 },
                new DeviceModel() { SubCategory = 0x15, Number = "2342-442", Type = ModelType.RemoteLinc, Name = "Insteon Mini Remote - Switch (869 MHz)" },
                new DeviceModel() { SubCategory = 0x16, Number = "2342-422", Type = ModelType.RemoteLinc, Name = "Insteon Mini Remote - 8 Scene (869 MHz)", ChannelCount = 8 },
                new DeviceModel() { SubCategory = 0x17, Number = "2342-532", Type = ModelType.RemoteLinc, Name = "Insteon Mini Remote - 4 Scene (921 MHz)", ChannelCount = 4 },
                new DeviceModel() { SubCategory = 0x18, Number = "2342-522", Type = ModelType.RemoteLinc, Name = "Insteon Mini Remote - 8 Scene (921 MHz)", ChannelCount = 8  },
                new DeviceModel() { SubCategory = 0x19, Number = "2342-542", Type = ModelType.RemoteLinc, Name = "Insteon Mini Remote - Switch (921 MHz)" },
                new DeviceModel() { SubCategory = 0x1A, Number = "2342-222", Type = ModelType.RemoteLinc, Name = "Insteon Mini Remote - 8 Scene (915 MHz)", ChannelCount = 8 },
                new DeviceModel() { SubCategory = 0x1B, Number = "2342-232", Type = ModelType.RemoteLinc, Name = "Insteon Mini Remote - 4 Scene (915 MHz)", ChannelCount = 5 },
                new DeviceModel() { SubCategory = 0x1C, Number = "2342-242", Type = ModelType.RemoteLinc, Name = "Insteon Mini Remote - Switch (915 MHz)" },
                new DeviceModel() { SubCategory = 0x1D, Number = "2992-222", Type = ModelType.Unknown, Name = "Range Extender" },
            }
        },
        new Category() /* 0x01 */
        {
            Name = "Dimmable Lighting Control",
            Notes = "Dimmable Light Switches, Dimmable Plug-In Modules",
            Models = new DeviceModel[]
            {
                new DeviceModel() { SubCategory = 0x00, Number = "2456D3", Type = ModelType.LampLinc, Name = "LampLinc 3-Pin" },
                new DeviceModel() { SubCategory = 0x01, Number = "2476D", Type = ModelType.SwitchLincDimmer, Name = "SwitchLinc Dimmer" },
                new DeviceModel() { SubCategory = 0x02, Number = "2475D", Type = ModelType.InlineLincDimmer, Name = "In-LineLinc Dimmer" },
                new DeviceModel() { SubCategory = 0x03, Number = "2876DB", Type = ModelType.SwitchLincDimmer, Name = "ICON Dimmer Switch" },
                new DeviceModel() { SubCategory = 0x04, Number = "2476DH", Type = ModelType.SwitchLincDimmer, Name = "SwitchLinc Dimmer (High Wattage)" },
                new DeviceModel() { SubCategory = 0x05, Number = "2484DWH8", Type = ModelType.KeypadLincDimmer, Name = "Keypad Countdown Timer w/ Dimmer" },
                new DeviceModel() { SubCategory = 0x06, Number = "2456D2", Type = ModelType.LampLinc, Name = "LampLinc Dimmer (2-Pin)" },
                new DeviceModel() { SubCategory = 0x07, Number = "2856D2B", Type = ModelType.LampLinc, Name = "ICON LampLinc" },
                new DeviceModel() { SubCategory = 0x08, Number = "2476DT", Type = ModelType.SwitchLincDimmer, Name = "SwitchLinc Dimmer Count-down Timer" },
                new DeviceModel() { SubCategory = 0x09, Number = "2486D", Type = ModelType.KeypadLincDimmer, Name = "KeypadLinc Dimmer", ChannelCount = 6 },
                new DeviceModel() { SubCategory = 0x0A, Number = "2886D", Type = ModelType.KeypadLincDimmer, Name = "Icon In-Wall Controller", ChannelCount = 6 },
                new DeviceModel() { SubCategory = 0x0B, Number = "2632-422", Type = ModelType.SwitchLincDimmer, Name = "Insteon Dimmer Module, France (869 MHz)" },
                new DeviceModel() { SubCategory = 0x0C, Number = "2486DWH8", Type = ModelType.KeypadLincDimmer, Name = "KeypadLinc Dimmer", ChannelCount = 8 },
                new DeviceModel() { SubCategory = 0x0D, Number = "2454D", Type = ModelType.SocketLinc, Name = "SocketLinc" },
                new DeviceModel() { SubCategory = 0x0E, Number = "2457D2", Type = ModelType.LampLinc, Name = "LampLinc (Dual-Band)" },
                new DeviceModel() { SubCategory = 0x0F, Number = "2632-432", Type = ModelType.SwitchLincDimmer, Name = "Insteon Dimmer Module, Germany (869 MHz)" },
                new DeviceModel() { SubCategory = 0x11, Number = "2632-442", Type = ModelType.SwitchLincDimmer, Name = "Insteon Dimmer Module, UK (869 MHz)" },
                new DeviceModel() { SubCategory = 0x12, Number = "2632-522", Type = ModelType.SwitchLincDimmer, Name = "Insteon Dimmer Module, Aus/NZ (921 MHz)" },
                new DeviceModel() { SubCategory = 0x13, Number = "2676D-B", Type = ModelType.SwitchLincDimmer, Name = "ICON SwitchLinc Dimmer Lixar/Bell Canada" },
                new DeviceModel() { SubCategory = 0x17, Number = "2466D", Type = ModelType.SwitchLincDimmer, Name = "ToggleLinc Dimmer" },
                new DeviceModel() { SubCategory = 0x18, Number = "2474D", Type = ModelType.SwitchLincDimmer, Name = "Icon SwitchLinc Dimmer Inline Companion" },
                new DeviceModel() { SubCategory = 0x19, Number = "2476D", Type = ModelType.SwitchLincDimmer, Name = "SwitchLinc Dimmer [with beeper]" },
                new DeviceModel() { SubCategory = 0x1A, Number = "2475D", Type = ModelType.InlineLincDimmer, Name = "In-LineLinc Dimmer [with beeper]" },
                new DeviceModel() { SubCategory = 0x1B, Number = "2486DWH6", Type = ModelType.KeypadLincDimmer, Name = "KeypadLinc Dimmer", ChannelCount = 6 },
                new DeviceModel() { SubCategory = 0x1C, Number = "2486DWH8", Type = ModelType.KeypadLincDimmer, Name = "KeypadLinc Dimmer", ChannelCount = 8 },
                new DeviceModel() { SubCategory = 0x1D, Number = "2476DH", Type = ModelType.SwitchLincDimmer, Name = "SwitchLinc Dimmer (High Wattage)[beeper]" },
                new DeviceModel() { SubCategory = 0x1E, Number = "2876DB", Type = ModelType.SwitchLincDimmer, Name = "ICON Switch Dimmer" },
                new DeviceModel() { SubCategory = 0x1F, Number = "2466Dx", Type = ModelType.SwitchLincDimmer, Name = "ToggleLinc Dimmer [with beeper]" },
                new DeviceModel() { SubCategory = 0x20, Number = "2477D", Type = ModelType.SwitchLincDimmer, Name = "SwitchLinc Dimmer (Dual-Band)" },
                new DeviceModel() { SubCategory = 0x21, Number = "2472D", Type = ModelType.OutletLinc, Name = "OutletLinc Dimmer (Dual-Band)" },
                new DeviceModel() { SubCategory = 0x22, Number = "2457D2X", Type = ModelType.LampLinc, Name = "LampLinc" },
                new DeviceModel() { SubCategory = 0x23, Number = "2457D2EZ", Type = ModelType.LampLinc, Name = "LampLinc Dual-Band EZ" },
                new DeviceModel() { SubCategory = 0x24, Number = "2474DWH", Type = ModelType.SwitchLincRelay, Name = "SwitchLinc 2-Wire Dimmer (RF)" },
                new DeviceModel() { SubCategory = 0x25, Number = "2475DA2", Type = ModelType.InlineLincDimmer, Name = "In-LineLinc 0-10VDC Dimmer/Dual-SwitchDB" },
                new DeviceModel() { SubCategory = 0x2D, Number = "2477DH", Type = ModelType.SwitchLincDimmer, Name = "SwitchLinc-Dimmer Dual-Band 1000W" },
                new DeviceModel() { SubCategory = 0x2E, Number = "2475F", Type = ModelType.FanLinc, Name = "FanLinc" },
                new DeviceModel() { SubCategory = 0x2F, Number = "2484DST6", Type = ModelType.KeypadLincDimmer, Name = "KeypadLinc Schedule Timer with Dimmer", ChannelCount = 6 },
                new DeviceModel() { SubCategory = 0x30, Number = "2476D", Type = ModelType.SwitchLincDimmer, Name = "SwitchLinc Dimmer" },
                new DeviceModel() { SubCategory = 0x31, Number = "2478D", Type = ModelType.SwitchLincDimmer, Name = "SwitchLinc Dimmer 240V-50/60Hz Dual-Band" },
                new DeviceModel() { SubCategory = 0x32, Number = "2475DA1", Type = ModelType.InlineLincDimmer, Name = "In-LineLinc Dimmer (Dual Band)" },
                new DeviceModel() { SubCategory = 0x34, Number = "2452-222", Name = "Insteon DIN Rail Dimmer (915 MHz)" },
                new DeviceModel() { SubCategory = 0x35, Number = "2442-222", Name = "Insteon Micro Dimmer (915 MHz)" },
                new DeviceModel() { SubCategory = 0x36, Number = "2452-422", Name = "Insteon DIN Rail Dimmer (869 MHz)" },
                new DeviceModel() { SubCategory = 0x37, Number = "2452-522", Name = "Insteon DIN Rail Dimmer (921 MHz)" },
                new DeviceModel() { SubCategory = 0x38, Number = "2442-422", Name = "Insteon Micro Dimmer (869 MHz)" },
                new DeviceModel() { SubCategory = 0x39, Number = "2442-522", Name = "Insteon Micro Dimmer (921 MHz)" },
                new DeviceModel() { SubCategory = 0x3A, Number = "2672-222", Type = ModelType.DimmableBulb, Name = "LED Bulb 240V (915 MHz) - Screw-in Base" },
                new DeviceModel() { SubCategory = 0x3B, Number = "2672-422", Type = ModelType.DimmableBulb, Name = "LED Bulb 240V Europe - Screw-in Base" },
                new DeviceModel() { SubCategory = 0x3C, Number = "2672-522", Type = ModelType.DimmableBulb, Name = "LED Bulb 240V Aus/NZ - Screw-in Base" },
                new DeviceModel() { SubCategory = 0x3D, Number = "2446-422", Type = ModelType.SwitchLincDimmer, Name = "Insteon Ballast Dimmer (869 MHz)" },
                new DeviceModel() { SubCategory = 0x3E, Number = "2446-522", Type = ModelType.SwitchLincDimmer, Name = "Insteon Ballast Dimmer (921 MHz)" },
                new DeviceModel() { SubCategory = 0x3F, Number = "2447-422", Type = ModelType.SwitchLincDimmer, Name = "Insteon Fixture Dimmer (869 MHz)" },
                new DeviceModel() { SubCategory = 0x40, Number = "2447-522", Type = ModelType.SwitchLincDimmer, Name = "Insteon Fixture Dimmer (921 MHz)" },
                new DeviceModel() { SubCategory = 0x41, Number = "2334-222", Type = ModelType.KeypadLincDimmer, Name = "Keypad Dimmer Dual-Band, 8 Button", ChannelCount = 8  },
                new DeviceModel() { SubCategory = 0x42, Number = "2334-232", Type = ModelType.KeypadLincDimmer, Name = "Keypad Dimmer Dual-Band, 6 Button", ChannelCount = 6  },
                new DeviceModel() { SubCategory = 0x49, Number = "2674-222", Type = ModelType.DimmableBulb, Name = "LED Bulb PAR38 US/Can - Screw-in Base" },
                new DeviceModel() { SubCategory = 0x4A, Number = "2674-422", Type = ModelType.DimmableBulb, Name = "LED Bulb PAR38 Europe - Screw-in Base" },
                new DeviceModel() { SubCategory = 0x4B, Number = "2674-522", Type = ModelType.DimmableBulb, Name = "LED Bulb PAR38 Aus/NZ - Screw-in Base" },
                new DeviceModel() { SubCategory = 0x4C, Number = "2672-432", Type = ModelType.DimmableBulb, Name = "LED Bulb 240V Europe - Bayonet Base" },
                new DeviceModel() { SubCategory = 0x4D, Number = "2672-532", Type = ModelType.DimmableBulb, Name = "LED Bulb 240V Aus/NZ - Bayonet Base" },
                new DeviceModel() { SubCategory = 0x4E, Number = "2674-432", Type = ModelType.DimmableBulb, Name = "LED Bulb PAR38 Europe - Bayonet Base" },
                new DeviceModel() { SubCategory = 0x4F, Number = "2674-532", Type = ModelType.DimmableBulb, Name = "LED Bulb PAR38 Aus/NZ - Bayonet Base" },
                new DeviceModel() { SubCategory = 0x50, Number = "2632-452", Type = ModelType.SwitchLincDimmer, Name = "Insteon Dimmer Module, Chile (915 MHz)" },
                new DeviceModel() { SubCategory = 0x51, Number = "2672-452", Type = ModelType.DimmableBulb, Name = "LED Bulb 240V (915 MHz) - Screw-in Base" },
            }
        },
        new Category() /* 0x02 */
        {
            Name = "Switched Lighting Control",
            Notes = "Relay Switches, Relay Plug-In Modules",
            Models = new DeviceModel[]
            {

                new DeviceModel() { SubCategory = 0x05, Number = "2486SWH8", Type = ModelType.KeypadLincRelay, Name = "KeypadLinc 8-button On/Off Switch", ChannelCount = 8 },
                new DeviceModel() { SubCategory = 0x06, Number = "2456S3E", Type = ModelType.ApplianceLinc, Name = "Outdoor ApplianceLinc" },
                new DeviceModel() { SubCategory = 0x07, Number = "2456S3T", Type = ModelType.TimerLinc, Name = "TimerLinc" },
                new DeviceModel() { SubCategory = 0x08, Number = "2473S", Type = ModelType.OutletLinc, Name = "OutletLinc" },
                new DeviceModel() { SubCategory = 0x09, Number = "2456S3", Type = ModelType.ApplianceLinc, Name = "ApplianceLinc (3-Pin)" },
                new DeviceModel() { SubCategory = 0x0A, Number = "2476S", Type = ModelType.SwitchLincRelay, Name = "SwitchLinc Relay" },
                new DeviceModel() { SubCategory = 0x0B, Number = "2876S", Type = ModelType.SwitchLincRelay, Name = "ICON On/Off Switch" },
                new DeviceModel() { SubCategory = 0x0C, Number = "2856S3", Type = ModelType.ApplianceLinc, Name = "Icon Appliance Module" },
                new DeviceModel() { SubCategory = 0x0D, Number = "2466S", Type = ModelType.SwitchLincRelay, Name = "ToggleLinc Relay" },
                new DeviceModel() { SubCategory = 0x0E, Number = "2476ST", Type = ModelType.SwitchLincRelay, Name = "SwitchLinc Relay Countdown Timer" },
                new DeviceModel() { SubCategory = 0x0F, Number = "2486SWH6", Type = ModelType.KeypadLincRelay, Name = "KeypadLinc On/Off", ChannelCount = 6 },
                new DeviceModel() { SubCategory = 0x10, Number = "2475S", Type = ModelType.InlineLincRelay, Name = "In-LineLinc Relay" },
                new DeviceModel() { SubCategory = 0x12, Number = "2474 S/D", Type = ModelType.InlineLincRelay, Name = "ICON In-lineLinc Relay Companion" },
                new DeviceModel() { SubCategory = 0x13, Number = "2676R-B", Type = ModelType.InlineLincRelay, Name = "ICON SwitchLinc Relay Lixar/Bell Canada" },
                new DeviceModel() { SubCategory = 0x14, Number = "2475S2", Type = ModelType.InlineLincRelay, Name = "In-LineLinc Relay with Sense" },
                new DeviceModel() { SubCategory = 0x15, Number = "2476SS", Type = ModelType.SwitchLincRelay, Name = "SwitchLinc Relay with Sense" },
                new DeviceModel() { SubCategory = 0x16, Number = "2876S", Type = ModelType.SwitchLincRelay, Name = "ICON On/Off Switch (25 max links)" },
                new DeviceModel() { SubCategory = 0x17, Number = "2856S3B", Type = ModelType.ApplianceLinc, Name = "ICON Appliance Module" },
                new DeviceModel() { SubCategory = 0x18, Number = "2494S220", Type = ModelType.SwitchLincRelay, Name = "SwitchLinc 220V Relay" },
                new DeviceModel() { SubCategory = 0x19, Number = "2494S220", Type = ModelType.SwitchLincRelay, Name = "SwitchLinc 220V Relay [with beeper]" },
                new DeviceModel() { SubCategory = 0x1A, Number = "2466Sx", Type = ModelType.SwitchLincRelay, Name = "ToggleLinc Relay [with Beeper]" },
                new DeviceModel() { SubCategory = 0x1C, Number = "2476S", Type = ModelType.SwitchLincRelay, Name = "SwitchLinc Relay" },
                new DeviceModel() { SubCategory = 0x1D, Number = "4101", Type = ModelType.SwitchLincRelay, Name = "Commercial Switch with relay" },
                new DeviceModel() { SubCategory = 0x1E, Number = "2487S", Type = ModelType.KeypadLincRelay, Name = "KeypadLinc On/Off (Dual-Band)", ChannelCount = 6 },
                new DeviceModel() { SubCategory = 0x1F, Number = "2475SDB", Type = ModelType.InlineLincRelay, Name = "In-LineLinc On/Off (Dual-Band)" },
                new DeviceModel() { SubCategory = 0x25, Number = "2484SWH8", Type = ModelType.KeypadLincRelay, Name = "KeypadLinc 8-Button Countdown On/Off Switch Timer", ChannelCount = 8 },
                new DeviceModel() { SubCategory = 0x26, Number = "2485SWH6", Type = ModelType.KeypadLincRelay, Name = "Keypad Schedule Timer with On/Off Switch" },
                new DeviceModel() { SubCategory = 0x29, Number = "2476ST", Type = ModelType.SwitchLincRelay, Name = "SwitchLinc Relay Countdown Timer" },
                new DeviceModel() { SubCategory = 0x2A, Number = "2477S", Type = ModelType.SwitchLincRelay, Name = "SwitchLinc Relay (Dual-Band)" },
                new DeviceModel() { SubCategory = 0x2B, Number = "2475SDB-50", Type = ModelType.InlineLincRelay, Name = "In-LineLinc On/Off (Dual Band, 50/60 Hz)" },
                new DeviceModel() { SubCategory = 0x2C, Number = "2487S", Type = ModelType.KeypadLincRelay, Name = "KeypadLinc On/Off (Dual-Band,50/60 Hz)", ChannelCount = 6 },
                new DeviceModel() { SubCategory = 0x2D, Number = "2633-422", Type = ModelType.SwitchLincRelay, Name = "Insteon On/Off Module, France (869 MHz)" },
                new DeviceModel() { SubCategory = 0x2E, Number = "2453-222", Name = "Insteon DIN Rail On/Off (915 MHz)" },
                new DeviceModel() { SubCategory = 0x2F, Number = "2443-222", Name = "Insteon Micro On/Off (915 MHz)" },
                new DeviceModel() { SubCategory = 0x30, Number = "2633-432", Name = "Insteon On/Off Module, Germany (869 MHz)" },
                new DeviceModel() { SubCategory = 0x31, Number = "2443-422", Name = "Insteon Micro On/Off (869 MHz)" },
                new DeviceModel() { SubCategory = 0x32, Number = "2443-522", Name = "Insteon Micro On/Off (921 MHz)" },
                new DeviceModel() { SubCategory = 0x33, Number = "2453-422", Name = "Insteon DIN Rail On/Off (869 MHz)" },
                new DeviceModel() { SubCategory = 0x34, Number = "2453-522", Name = "Insteon DIN Rail On/Off (921 MHz)" },
                new DeviceModel() { SubCategory = 0x35, Number = "2633-442", Name = "Insteon On/Off Module, UK (869 MHz)" },
                new DeviceModel() { SubCategory = 0x36, Number = "2633-522", Name = "Insteon On/Off Module, Aus/NZ (921 MHz)" },
                new DeviceModel() { SubCategory = 0x37, Number = "2635-222", Name = "Insteon On/Off Module, US (915 MHz)" },
                new DeviceModel() { SubCategory = 0x38, Number = "2634-222", Name = "On/Off Outdoor Module (Dual-Band)" },
                new DeviceModel() { SubCategory = 0x39, Number = "2663-222", Name = "On/Off Outlet" },
                new DeviceModel() { SubCategory = 0x3A, Number = "2633-452", Name = "Insteon On/Off Module, Chile (915 MHz)" },
            }
        },
        new Category() /* 0x03 */
        {
            Name = "Network Bridge",
            Notes = "PowerLinc Controllers, TRex, Lonworks, ZigBee, etc.",
            Models = new DeviceModel[] {
                new DeviceModel() { SubCategory = 0x01, Number = "2414S", Type = ModelType.PowerLinc, Name = "PowerLinc Serial Controller" },
                new DeviceModel() { SubCategory = 0x02, Number = "2414U", Type = ModelType.PowerLinc, Name = "PowerLinc USB Controller" },
                new DeviceModel() { SubCategory = 0x03, Number = "2814S", Type = ModelType.PowerLinc, Name = "ICON PowerLinc Serial" },
                new DeviceModel() { SubCategory = 0x04, Number = "2814U", Type = ModelType.PowerLinc, Name = "ICON PowerLinc USB" },
                new DeviceModel() { SubCategory = 0x05, Number = "2412S", Type = ModelType.PowerLinc, Name = "PowerLinc Serial Modem" },
                new DeviceModel() { SubCategory = 0x06, Number = "2411R", Name = "IRLinc Receiver" },
                new DeviceModel() { SubCategory = 0x07, Number = "2411T", Name = "IRLinc Transmitter" },
                new DeviceModel() { SubCategory = 0x09, Number = "2600RF", Name = "SmartLabs RF Developer’s Board" },
                new DeviceModel() { SubCategory = 0x0A, Number = "2410S", Type = ModelType.SerialLinc, Name = "SeriaLinc - Insteon to RS232" },
                new DeviceModel() { SubCategory = 0x0B, Number = "2412U", Type = ModelType.PowerLinc, Name = "PowerLinc USB Modem" },
                new DeviceModel() { SubCategory = 0x0F, Number = "EZX10IR", Name = "EZX10IR X10 IR Receiver" },
                new DeviceModel() { SubCategory = 0x10, Number = "2412N", Type = ModelType.SmartLinc, Name = "SmartLinc" },
                new DeviceModel() { SubCategory = 0x11, Number = "2413S", Type = ModelType.PowerLinc, Name = "PowerLinc Serial Modem (Dual Band)" },
                new DeviceModel() { SubCategory = 0x13, Number = "2412UH", Type = ModelType.PowerLinc, Name = "PowerLinc USB Modem for HouseLinc" },
                new DeviceModel() { SubCategory = 0x14, Number = "2412SH", Type = ModelType.PowerLinc, Name = "PowerLinc Serial Modem for HouseLinc" },
                new DeviceModel() { SubCategory = 0x15, Number = "2413U", Type = ModelType.PowerLinc, Name = "PowerLinc USB Modem (Dual Band)" },
                new DeviceModel() { SubCategory = 0x18, Number = "2243-222", Name = "Insteon Central Controller (915 MHz)" },
                new DeviceModel() { SubCategory = 0x19, Number = "2413SH", Type = ModelType.PowerLinc, Name = "PowerLinc Serial Modem for HL(Dual Band)" },
                new DeviceModel() { SubCategory = 0x1A, Number = "2413UH", Type = ModelType.PowerLinc, Name = "PowerLinc USB Modem for HL (Dual Band)" },
                new DeviceModel() { SubCategory = 0x1B, Number = "2423A4", Name = "iGateway" },
                new DeviceModel() { SubCategory = 0x1C, Number = "2423A7", Name = "iGateway 2.0" },
                new DeviceModel() { SubCategory = 0x1E, Number = "2412S", Type = ModelType.PowerLinc, Name = "PowerLincModemSerial w/o EEPROM(w/o RF)" },
                new DeviceModel() { SubCategory = 0x1F, Number = "2448A7", Name = "USB Adapter - Domestically made" },
                new DeviceModel() { SubCategory = 0x20, Number = "2448A7", Name = "USB Adapter" },
                new DeviceModel() { SubCategory = 0x21, Number = "2448A7H", Name = "Portable USB Adapter for HouseLinc" },
                new DeviceModel() { SubCategory = 0x23, Number = "2448A7H", Name = "Portable USB Adapter for HouseLinc" },
                new DeviceModel() { SubCategory = 0x24, Number = "2448A7T", Name = "TouchLinc" },
                new DeviceModel() { SubCategory = 0x27, Number = "2448A7T", Name = "TouchLinc" },
                new DeviceModel() { SubCategory = 0x28, Number = "2413Gxx", Name = "Global PLM, Dual Band (915 MHz)" },
                new DeviceModel() { SubCategory = 0x29, Number = "2413SAD", Type = ModelType.PowerLinc, Name = "PowerLinc Serial Modem (Dual Band) RF OFF, Auto Detect 128K" },
                new DeviceModel() { SubCategory = 0x2B, Number = "2242-222", Type = ModelType.Hub, Name = "Insteon Hub (915 MHz) - no RF", ChannelCount=255 },
                new DeviceModel() { SubCategory = 0x2E, Number = "2242-422", Type = ModelType.Hub, Name = "Insteon Hub (EU - 869 MHz)", ChannelCount=255 },
                new DeviceModel() { SubCategory = 0x2F, Number = "2242-522", Type = ModelType.Hub, Name = "Insteon Hub (921 MHz)", ChannelCount=255 },
                new DeviceModel() { SubCategory = 0x30, Number = "2242-442", Type = ModelType.Hub, Name = "Insteon Hub (UK - 869 MHz)", ChannelCount=255 },
                new DeviceModel() { SubCategory = 0x31, Number = "2242-232", Type = ModelType.Hub, Name = "Insteon Hub (Plug-In Version)", ChannelCount=255 },
                new DeviceModel() { SubCategory = 0x33, Number = "2245-222", Type = ModelType.Hub, Name = "Insteon Hub II (915 MHz)", ChannelCount=255 },
                new DeviceModel() { SubCategory = 0x37, Number = "2242-222", Type = ModelType.Hub, Name = "Insteon Hub (915 MHz) - RF", ChannelCount=255 },
            }
        },
        new Category() /* 0x04 */
        {
            Name = "Irrigation Control",
            Notes = "Irrigation Management, Sprinkler Controllers",
            Models = new DeviceModel[]
            {
                new DeviceModel() { SubCategory = 0x00, Number = "31270", Name = "Compacta EZRain Sprinkler Controller" },
            }
        },
        new Category() /* 0x05 */
        {
            Name = "Climate Control",
            Notes = "Heating, Air conditioning, Exhausts Fans, Ceiling Fans, Indoor Air Quality",
            Models = new DeviceModel[]
            {
                new DeviceModel() { SubCategory = 0x00, Number = "2670IAQ-80", Name = "Broan SMSC080 Exhaust Fan (no beeper)" },
                new DeviceModel() { SubCategory = 0x02, Number = "2670IAQ-110", Name = "Broan SMSC110 Exhaust Fan (no beeper)" },
                new DeviceModel() { SubCategory = 0x03, Number = "2441V", Name = "Thermostat Adapter" },
                new DeviceModel() { SubCategory = 0x07, Number = "2441ZT", Name = "Insteon Wireless Thermostat" },
                new DeviceModel() { SubCategory = 0x0A, Number = "2441ZTH", Name = "Insteon Wireless Thermostat (915 MHz)" },
                new DeviceModel() { SubCategory = 0x0B, Number = "2441TH", Name = "Insteon Thermostat (915 MHz)" },
                new DeviceModel() { SubCategory = 0x0C, Number = "2670IAQ-80", Name = "Broan SMSC080 Switch for 80CFM Fans" },
                new DeviceModel() { SubCategory = 0x0D, Number = "2670IAQ-110", Name = "Broan SMSC110 Switch for 110CFM Fans" },
                new DeviceModel() { SubCategory = 0x0E, Number = "2491TxE", Name = "Integrated Remote Control Thermostat" },
                new DeviceModel() { SubCategory = 0x0F, Number = "2732-422", Name = "Insteon Thermostat (869 MHz)" },
                new DeviceModel() { SubCategory = 0x10, Number = "2732-522", Name = "Insteon Thermostat (921 MHz)" },
                new DeviceModel() { SubCategory = 0x11, Number = "2732-432", Name = "Insteon Zone Thermostat (869 MHz)" },
                new DeviceModel() { SubCategory = 0x12, Number = "2732-532", Name = "Insteon Zone Thermostat (921 MHz)" },
                new DeviceModel() { SubCategory = 0x13, Number = "2732-242", Name = "Heat Pump Thermostat - US/Can (915MHz)" },
                new DeviceModel() { SubCategory = 0x14, Number = "2732-242", Name = "Heat Pump Thermostat - Europe (869.85MHz)" },
                new DeviceModel() { SubCategory = 0x15, Number = "2732-242", Name = "Heat Pump Thermostat - Aus/NZ (921MHz" },
            }
        },
        new Category() /* 0x06 */
        {
            Name = "Pool and Spa Control",
            Notes = "Pumps, Heaters, Chemicals"
        },
        new Category() /* 0x07 */
        {
            Name = "Sensors and Actuators",
            Notes = "Sensors, Contact Closures",
            Models = new DeviceModel[]
            {
                new DeviceModel() { SubCategory = 0x00, Number = "2450", Type = ModelType.LowVoltageLinc, Name = "I/OLinc" },
                new DeviceModel() { SubCategory = 0x03, Number = "31274", Type = ModelType.LowVoltageLinc, Name = "Compacta EZIO2X4 #5010D " },
                new DeviceModel() { SubCategory = 0x05, Number = "31275", Type = ModelType.LowVoltageLinc, Name = "Compacta EZSnsRF RcvrIntrfc Dakota Alert" },
                new DeviceModel() { SubCategory = 0x07, Number = "31280", Type = ModelType.LowVoltageLinc, Name = "EZIO6I (6 inputs)" },
                new DeviceModel() { SubCategory = 0x08, Number = "31283", Type = ModelType.LowVoltageLinc, Name = "EZIO4O (4 relay outputs)" },
                new DeviceModel() { SubCategory = 0x09, Number = "2423A5", Type = ModelType.LowVoltageLinc, Name = "SynchroLinc" },
                new DeviceModel() { SubCategory = 0x0C, Number = "2448A5", Type = ModelType.LowVoltageLinc, Name = "Lumistat" },
                new DeviceModel() { SubCategory = 0x0D, Number = "2450", Type = ModelType.LowVoltageLinc, Name = "I/OLinc 50/60Hz Auto Detect" },
                new DeviceModel() { SubCategory = 0x0E, Number = "2248-222", Type = ModelType.LowVoltageLinc, Name = "I/O Module - US (915 MHz)" },
                new DeviceModel() { SubCategory = 0x0F, Number = "2248-422", Type = ModelType.LowVoltageLinc, Name = "I/O Module - EU (869.85 MHz)" },
                new DeviceModel() { SubCategory = 0x10, Number = "2248-442", Type = ModelType.LowVoltageLinc, Name = "I/O Module - UK (869.85 MHz)" },
                new DeviceModel() { SubCategory = 0x11, Number = "2248-522", Type = ModelType.LowVoltageLinc, Name = "I/O Module - AUS (921 MHz)" },
                new DeviceModel() { SubCategory = 0x12, Number = "2822-222", Type = ModelType.LowVoltageLinc, Name = "IOLinc Dual-Band - US" },
                new DeviceModel() { SubCategory = 0x13, Number = "2822-422", Type = ModelType.LowVoltageLinc, Name = "IOLinc Dual-Band - EU" },
                new DeviceModel() { SubCategory = 0x14, Number = "2822-442", Type = ModelType.LowVoltageLinc, Name = "IOLinc Dual-Band - UK" },
                new DeviceModel() { SubCategory = 0x15, Number = "2822-522", Type = ModelType.LowVoltageLinc, Name = "IOLinc Dual-Band - AUS/NZ" },
                new DeviceModel() { SubCategory = 0x16, Number = "2822-222", Type = ModelType.LowVoltageLinc, Name = "Low Voltage/Contact Closure Interface (Dual Band) - US" },
                new DeviceModel() { SubCategory = 0x17, Number = "2822-422", Type = ModelType.LowVoltageLinc, Name = "Low Voltage/Contact Closure Interface (Dual Band) - EU" },
                new DeviceModel() { SubCategory = 0x18, Number = "2822-442", Type = ModelType.LowVoltageLinc, Name = "Low Voltage/Contact Closure Interface (Dual Band) - UK" },
                new DeviceModel() { SubCategory = 0x19, Number = "2822-522", Type = ModelType.LowVoltageLinc, Name = "Low Voltage/Contact Closure Interface (Dual Band) - AUS/NZ" },
            }
        },
        new Category() /* 0x08 */
        {
            Name = "Home Entertainment",
            Notes = "Audio/Video Equipment"
        },
        new Category() /* 0x09 */
        {
            Name = "Energy Management",
            Notes = "Electricity, Water, Gas Consumption, Leak Monitors",
            Models = new DeviceModel[]
            {
                new DeviceModel() { SubCategory = 0x07, Number = "2423A1", Name = "iMeter Solo" },
                new DeviceModel() { SubCategory = 0x08, Number = "2423A2", Name = "iMeter Home (Breaker Panel)" },
                new DeviceModel() { SubCategory = 0x09, Number = "2423A3", Name = "iMeter Home (Meter)" },
                new DeviceModel() { SubCategory = 0x0A, Number = "2477SA1", Name = "220/240V 30A Load Controller NO (DB)" },
                new DeviceModel() { SubCategory = 0x0B, Number = "2477SA2", Name = "220/240V 30A Load Controller NC (DB)" },
                new DeviceModel() { SubCategory = 0x0C, Number = "2630A1", Name = "GE Water Heater U-SNAP module" },
                new DeviceModel() { SubCategory = 0x0D, Number = "2448A2", Name = "Energy Display" },
                new DeviceModel() { SubCategory = 0x0E, Number = "2423A6", Name = "Power Strip with iMeter and SynchroLinc" },
                new DeviceModel() { SubCategory = 0x11, Number = "2423A8", Name = "Insteon Digital Meter Reader" },
            }
        },
        new Category() /* 0x0A */ { Name = "Built-In Appliance Control", Notes = "White Goods, Brown Goods" },
        new Category() /* 0x0B */ { Name = "Plumbing", Notes = "Faucets, Showers, Toilets" },
        new Category() /* 0x0C */ { Name = "Communication", Notes = "Telephone System Controls, Intercoms" },
        new Category() /* 0x0D */ { Name = "Computer Control", Notes = "PC On/Off, UPS Control, App Activation, Remote Mouse, Keyboards" },
        new Category() /* 0x0E */
        {
            Name = "Window Coverings",
            Notes = "Drapes, Blinds, Awnings",
            Models = new DeviceModel[]
            {
                new DeviceModel() { SubCategory = 0x00, Number = "318276I", Name = "Somfy Drape Controller RF Bridge" },
                new DeviceModel() { SubCategory = 0x01, Number = "2444-222", Name = "Insteon Micro Open/Close (915 MHz)" },
                new DeviceModel() { SubCategory = 0x02, Number = "2444-422", Name = "Insteon Micro Open/Close (869 MHz)" },
                new DeviceModel() { SubCategory = 0x03, Number = "2444-522", Name = "Insteon Micro Open/Close (921 MHz)" },
                new DeviceModel() { SubCategory = 0x04, Number = "2772-222", Name = "Window Shade Kit - US" },
                new DeviceModel() { SubCategory = 0x05, Number = "2772-422", Name = "Window Shade Kit - EU" },
                new DeviceModel() { SubCategory = 0x06, Number = "2772-522", Name = "Window Shade Kit - AUS/NZ" },
            }
        },
        new Category() /* 0x0F */
        {
            Name = "Access Control",
            Notes = "Automatic Doors, Gates, Windows, Locks",
            Models = new DeviceModel[]
            {
                new DeviceModel() { SubCategory = 0x06, Number = "2458A1", Name = "MorningLinc" },
            }
        },
        new Category() /* 0x10 */
        {
            Name = "Security, Health, Safety",
            Notes = "Door and Window Sensors, Motion Sensors, Scales",
            Models = new DeviceModel[]
            {
                new DeviceModel() { SubCategory = 0x01, Number = "2842-222", Name = "Motion Sensor - US (915 MHz)" },
                new DeviceModel() { SubCategory = 0x02, Number = "2843-222", Name = "Insteon Open/Close Sensor (915 MHz)" },
                new DeviceModel() { SubCategory = 0x04, Number = "2842-422", Name = "Insteon Motion Sensor (869 MHz)" },
                new DeviceModel() { SubCategory = 0x05, Number = "2842-522", Name = "Insteon Motion Sensor (921 MHz)" },
                new DeviceModel() { SubCategory = 0x06, Number = "2843-422", Name = "Insteon Open/Close Sensor (869 MHz)" },
                new DeviceModel() { SubCategory = 0x07, Number = "2843-522", Name = "Insteon Open/Close Sensor (921 MHz)" },
                new DeviceModel() { SubCategory = 0x08, Number = "2852-222", Name = "Leak Sensor - US (915 MHz)" },
                new DeviceModel() { SubCategory = 0x09, Number = "2843-232", Name = "Insteon Door Sensor" },
                new DeviceModel() { SubCategory = 0x0A, Number = "2982-222", Name = "Smoke Bridge" },
                new DeviceModel() { SubCategory = 0x0D, Number = "2852-422", Name = "Leak Sensor - EU (869 MHz)" },
                new DeviceModel() { SubCategory = 0x0E, Number = "2852-522", Name = "Leak Sensor - AUS/NZ (921 MHz)" },
                new DeviceModel() { SubCategory = 0x11, Number = "2845-222", Name = "Door Sensor II (915 MHz)" },
                new DeviceModel() { SubCategory = 0x14, Number = "2845-422", Name = "Door Sensor II (869 MHz)" },
                new DeviceModel() { SubCategory = 0x15, Number = "2845-522", Name = "Door Sensor II (921 MHz)" },
            }
        },
        new Category() /* 0x11 */ { Name = "Surveillance", Notes = "Video Camera Control, Time-lapse Recorders, Security System Links" },
        new Category() /* 0x12 */ { Name = "Automotive", Notes = "Remote Starters, Car Alarms, Car Door Locks" },
        new Category() /* 0x13 */ { Name = "Pet Care", Notes = "Pet Feeders, Trackers" },
        new Category() /* 0x14 */ { Name = "Toys", Notes = "Model Trains, Robots" },
        new Category() /* 0x15 */ { Name = "Timekeeping", Notes = "Clocks, Alarms, Timers" },
        new Category() /* 0x16 */ { Name = "Holiday", Notes = "Christmas Lights, Displays" },
        new Category() /* 0x17 */ { Name = "Low Voltage", Notes = "Low Voltage Relay" },
    };

    static DeviceKind()
    {
        Debug.Assert((int)CategoryId.Count == Categories.Length);
    }
};
