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

/// <summary>
/// This is the root of the tree containing the model
/// It corresponds to the "settings" element in houselinc.xml
/// It contains Model ("insteon" in houselinc.xml) which is the root of the insteon model
/// </summary>
[Serializable]
[XmlType("settings")]
public sealed class HLSettings
{
    public HLSettings() { }

    public HLSettings(House house)
    {
        Version = house.Version!;
        HouselincApplication = new HLApplication(house);
        Insteon = new HLInsteon(house);
    }

    internal House BuildModel()
    {
        var house = new House()
        {
            Name = HouselincApplication.BuildModel().name!,
            Version = Version,
            HouseLocation = HouselincApplication?.BuildModel().location ?? null!,
        };
        (house.Gateways, house.Devices, house.Scenes) = Insteon.BuildModel(house);
        return house;
    }

    [XmlAttribute("version")]
    public string Version = null!;

    [XmlElement("application")]
    public HLApplication HouselincApplication = null!;

    [XmlElement("insteon")]
    public HLInsteon Insteon = null!;
}
