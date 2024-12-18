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

// This class represents a scene not implemented by the hub, 
// but instead by creating links between each controller and responder
[XmlType("scene")]
public sealed class HLScene
{
    public HLScene() { }

    public HLScene(Scene scene)
    {
        Id = scene.Id;
        Name = scene.Name;
        Room = scene.Room;
        LastTrigger = scene.LastTrigger;
        Notes = scene.Notes?.NullIfEmpty() ?? null;
        Members = new HLSceneMembers(scene.Members);
    }

    public Scene BuildModel(Scenes scenes)
    {
        // Converts a name of format "room - name" to Room and Name
        //var split = Name.Split('-');
        //if (split.Length > 1)
        //{
        //    Name = split[1].Trim();
        //    Room = split[0].Trim();
        //}
        var scene = new Scene(scenes, Name, Id)
        {
            Room = Room,
            LastTrigger = LastTrigger,
            Notes = Notes,
        };
        scene.Members = Members.BuildModel(scene);
        return scene;
    }

    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("name")]
    public string Name { get; set; } = null!;

    [XmlAttribute("room")]
    public string? Room { get; set; } = null!;

    [XmlAttribute("lastTrigger")]
    public string? LastTrigger { get; set; } = null!;

    [XmlAttribute("notes")]
    public string? Notes { get; set; } = null!;

    [XmlArray("members")]
    public HLSceneMembers Members { get; set; } = null!;
}
