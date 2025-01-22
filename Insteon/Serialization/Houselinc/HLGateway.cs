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
using Insteon.Base;
using Insteon.Model;

namespace Insteon.Serialization.Houselinc;

// Represents the "gateway" node in the houselinc file
// Provides gateway/hub state and functionality such as
// - IP address, port, hostname
// - user credentials to the gateway
// - can return an HttpClient to the gateway
// - stores and retrieves credentials from the credential store
[XmlType("gateway")]
public sealed class HLGateway
{
#pragma warning disable CS8618
    // The default constructor is only here to satisfy the XML deserializer
    // which then initialize the object by setting each XML property
    public HLGateway() { }
#pragma warning restore CS8618

    public HLGateway(Gateway gateway)
    {
        IP = new HLGatewayIP(gateway.HostName, gateway.IPAddress, gateway.Port, gateway.MacAddress);
        InsteonID = gateway.DeviceId;
    }

    public Gateway BuildModel(House house)
    {
        var gateway = new Gateway(house)
        {
            MacAddress = IP.Mac,
            HostName = IP.HostName,
            IPAddress = IP.Address,
            Port = IP.Port,
            DeviceId = InsteonID,
        };
        return gateway;
    }

    [XmlAttribute("insteonID")]
    public string InsteonIDSerialized
    {
        get
        {
            return (InsteonID ?? InsteonID.Null).ToString();
        }

        set
        {
            if (value != "")
            {
                InsteonID = new InsteonID(value);
            }
        }
    }

    [XmlIgnore]
    public InsteonID InsteonID { get; set; }

    [XmlElement(ElementName = "ip")]
    public HLGatewayIP IP { get; set; }

    public sealed class HLGatewayIP
    {
#pragma warning disable CS8618
        // The default constructor is only here to satisfy the XML deserializer
        // which then initialize the object by setting each XML property
        public HLGatewayIP()
        {
        }
#pragma warning restore CS8618

        public HLGatewayIP(string hostname, string address, string port, string mac)
        {
            HostName = hostname;
            Address = address;
            Port = port;
            Mac = mac;
        }

        [XmlAttribute(AttributeName = "hostname")]
        public string HostName { get; set; }

        [XmlAttribute(AttributeName = "port")]
        public string Port { get; set; }

        [XmlAttribute(AttributeName = "address")]
        public string Address { get; set; }

        [XmlAttribute(AttributeName = "mac")]
        public string Mac { get; set; }
    }
}
