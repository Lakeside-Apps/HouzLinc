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

using Common;
using System.Xml.Serialization;
using Insteon.Model;

namespace Insteon.Serialization.Houselinc;

[XmlType("member")]
public sealed class HLSceneMember
{
    public HLSceneMember() { }

    public HLSceneMember(SceneMember member)
    {
        this.InsteonID = member.DeviceId;
        this.Group = member.Group;
        this.IsController = member.IsController;
        this.IsResponder = member.IsResponder;
        this.Data1 = member.Data1;
        this.Data2 = member.Data2;
        this.Data3 = member.Data3;
        this.Status = member.Status;
        this.Tag = member.Tag;
    }

    public SceneMember BuildModel(Scene scene)
    {
        SceneMember member = new SceneMember(scene)
        {
            DeviceId = InsteonID,
            Group = Group,
            IsController = IsController,
            IsResponder = IsResponder,
            Data1 = Data1,
            Data2 = Data2,
            Data3 = Data3,
            Status = Status,
            Tag = Tag,
        };
        return member;
    }

    [XmlAttribute("iid")]
    public string InsteonIDSerialized
    {
        get
        {
            return InsteonID.ToString();
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
    public InsteonID InsteonID { get; set; } = InsteonID.Null;

    [XmlAttribute("group")]
    public byte Group { get; set; }

    [XmlAttribute("controller")]
    public string? ControllerSerialized { get => (IsController ? "1" : null); set => IsController = (value == "1"); }
    [XmlIgnore]
    public bool IsController { get; set; }

    [XmlAttribute("responder")]
    public string? ResponderSerialized { get => (IsResponder ? "1" : null); set => IsResponder = (value == "1"); }
    [XmlIgnore]
    public bool IsResponder { get; set; }

    [XmlAttribute("data1")]
    public byte Data1 { get; set; }

    [XmlAttribute("data2")]
    public byte Data2 { get; set; }

    [XmlAttribute("data3")]
    public byte Data3 { get; set; }

    [XmlAttribute("status")]
    public string Status { get; set; } = null!;

    [XmlAttribute("tag")]
    public int Tag { get; set; }
}
