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

[XmlType("location")]
public sealed class HLHouseLocation
{
#pragma warning disable CS8618
    // The default constructor is only here to satisfy the XML deserializer
    // which then initialize the object by setting each XML property
    public HLHouseLocation() { }
#pragma warning restore CS8618

    public HLHouseLocation(HouseLocation? loc)
    {
        State = loc?.State ?? string.Empty;
        City = loc?.City ?? string.Empty;
        Longitude = loc?.Longitude ?? string.Empty;
        Latitude = loc?.Latitude ?? string.Empty;
    }

    public HouseLocation BuildModel()
    {
        var loc = new HouseLocation()
        {
            State = State,
            City = City,
            Latitude = Latitude,
            Longitude = Longitude,
        };
        return loc;
    }

    [XmlAttribute("state")]
    public string State;

    [XmlAttribute("city")]
    public string City;

    [XmlAttribute("lon")]
    public string Longitude;

    [XmlAttribute("lat")]
    public string Latitude;
}
