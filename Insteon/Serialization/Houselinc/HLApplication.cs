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

[XmlType("application")]
public sealed class HLApplication
{
    public HLApplication() { }
    public HLApplication(House house)
    {
        houseLocation = house.HouseLocation != null ? new HLHouseLocation(house.HouseLocation) : null;
        houseName = house.Name != null ? new HLHouseName(house.Name) : null;
    }

    public (HouseLocation? location, string? name) BuildModel()
    {
        return (houseLocation?.BuildModel() ?? null, houseName?.BuildModel() ?? null);
    }

    [XmlElement("location")]
    public HLHouseLocation? houseLocation;

    [XmlElement("house")]
    public HLHouseName? houseName;
}
