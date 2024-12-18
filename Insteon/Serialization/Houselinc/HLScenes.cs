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
using Insteon.Model;

namespace Insteon.Serialization.Houselinc;

public sealed class HLScenesXMLWrapper
{
    public HLScenesXMLWrapper() { }

    public HLScenesXMLWrapper(Scenes scenes)
    {
        Scenes = new HLScenes(scenes);
        NextSceneID = scenes.NextSceneID;
    }

    public Scenes BuildModel(House house)
    {
        Scenes scenes = Scenes.BuildModel(house);
        scenes.NextSceneID = NextSceneID;
        return scenes;
    }

    [XmlAttribute("nextSceneID")]
    public int NextSceneID { get; set; }

    [XmlElement("scene")]
    public HLScenes Scenes { get; set; } = null!;
}

// Represents the list of scenes ("scenes" node) in houselinc.xml

public sealed class HLScenes : List<HLScene>
{
    public HLScenes() { }
    
    public HLScenes(Scenes scenes)
    {
        foreach(Scene scene in scenes)
        {
            Add(new HLScene(scene));
        }
    }

    public Scenes BuildModel(House house)
    {
        Scenes scenes = new Scenes(house);
        foreach(var hlScene in this)
        {
            scenes.Add(hlScene.BuildModel(scenes));
        }
        return scenes;
    }

    [XmlAttribute]
    private int NextSceneId { get; set; }
}


