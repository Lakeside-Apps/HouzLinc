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

using Insteon.Model;
using System.Xml.Serialization;

namespace Insteon.Serialization.Houselinc;

[XmlType("insteon")]
public sealed class HLInsteon
{
    public HLInsteon() { }

    public HLInsteon(House house)
    {
        Gateways = new HLGateways(house.Gateways);
        Devices = new HLDevices(house.Devices);
        ScenesXMLWrapper = new HLScenesXMLWrapper(house.Scenes);
    }

    public (Gateways gateway, Devices devices, Scenes scenes) BuildModel(House house)
    {
        var gateways = Gateways.BuildModel(house);
        var devices = Devices.BuildModel(house);
        var scenes = ScenesXMLWrapper?.BuildModel(house) ?? new Scenes(house);
        return (gateways, devices, scenes);
    }

    [XmlArray("active_gateways")]
    public HLGateways Gateways { get; set; } = null!;

    [XmlArray("active_devices")]
    public HLDevices Devices { get; set; } = null!;

    // XML serialization does not handle attributes on XmlArray types, so we use XMLElement 
    // instead and Scenes is not a list of scenes but instead contains a list of scenes
    [XmlElement("scenes")]
    public HLScenesXMLWrapper ScenesXMLWrapper { get; set; } = null!;
}
