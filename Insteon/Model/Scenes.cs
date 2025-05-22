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
using System.Diagnostics.CodeAnalysis;

namespace Insteon.Model;

/// <summary>
/// Represents the list of scenes in the model
/// </summary>
public sealed class Scenes : OrderedKeyedList<Scene>
{
    public Scenes(House house)
    {
        House = house;
    }

    public House House { get; init; }

    /// <summary>
    /// Id of the next scene to be created
    /// </summary>
    internal int NextSceneID 
    {
        get => nextSceneID;
        set 
        {
            if (value == nextSceneID) return;
            nextSceneID = value;
            observers.ForEach(o => o.ScenesPropertyChanged(this));
        }
    }
    private int nextSceneID;

    // Copy state from another Scenes object in a manner where properties and
    // collection change notifications are raised to observers
    internal void CopyFrom(Scenes fromScenes)
    {
        NextSceneID = fromScenes.NextSceneID;

        var fromScene2 = new Scenes(House);
        foreach (var device in fromScenes)
        {
            fromScene2.Add(device);
        }

        var scenesToRemove = new List<Scene>();
        foreach (var scene in this)
        {
            if (fromScene2.TryGetEntry(scene.Id, out var fromScene))
            {
                scene.CopyFrom(fromScene);
                fromScene2.Remove(fromScene);
            }
            else
            {
                scenesToRemove.Add(scene);
            }
        }

        foreach (var scene in scenesToRemove)
        {
            Remove(scene);
        }

        foreach (var scene in fromScene2)
        {
            var newScene = new Scene(this, scene.Id);
            Add(newScene);
            newScene.CopyFrom(scene);
        }
    }

    internal bool IsIdenticalTo(Scenes scenes)
    {
        if (NextSceneID != scenes.NextSceneID) return false;
        if (Count != scenes.Count) return false;
        for (var i = 0; i < Count; i++)
        {
            if (!this[i].IsIdenticalTo(scenes[i])) return false;
        }
        return true;
    }

    internal void OnDeserialized()
    {
        foreach(Scene scene in this)
        {
            scene.OnDeserialized();
        }

        AddObserver(House.ModelObserver);
    }

    /// <summary>
    /// For observers to subscribe to change notifications
    /// </summary>
    /// <param name="observer"></param>
    public Scenes AddObserver(IScenesObserver observer)
    {
        observers.Add(observer);
        return this;
    }

    /// <summary>
    /// For observers to unsubscribe to change notifications
    /// </summary>
    /// <param name="observer"></param>
    public Scenes RemoveObserver(IScenesObserver observer)
    {
        observers.Remove(observer);
        return this;
    }

    private List<IScenesObserver> observers = new List<IScenesObserver>();

    /// <summary>
    /// Get a scene object by Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Scene? GetSceneById(int id)
    {
        if (TryGetEntry(id, out Scene? scene))
        {
            return scene;
        }

        return null;
    }

    /// <summary>
    /// Same as above with Try pattern
    /// </summary>
    /// <param name="id"></param>
    /// <param name="scene"></param>
    /// <returns></returns>
    public bool TryGetSceneById(int id, [NotNullWhen(true)] out Scene? scene)
    {
        return TryGetEntry(id, out scene);
    }

    /// <summary>
    /// Add a new scene with a given name, with no scene members
    /// The scene Id is automatically generated
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Scene AddNewScene(string name)
    {
        // Find first available scene Id
        // Sometimes NextSceneId from the persisted model might be wrong,
        // ensure we are using an non yet used Id
        int nextSceneId = NextSceneID;
        while (GetSceneById(nextSceneId) != null) {nextSceneId++;}
        NextSceneID = nextSceneId;

        // Now create and add the new Scene
        Scene scene = new Scene(this, name, NextSceneID++);
        scene.AddObserver(House.ModelObserver);
        Add(scene);
        return scene;
    }

    /// <summary>
    /// Remove a scene and the links associated to it
    /// </summary>
    /// <param name="scene"></param>
    public void RemoveScene(Scene scene)
    {
        // This will removes all the links associated to the scene
        scene.RemoveAllMembers();

        // Now remove the scene
        Remove(scene);
        if (scene.Id == NextSceneID - 1)
        {
            NextSceneID--;
        }
    }

    /// <summary>
    /// Remove a scene by Id
    /// </summary>
    /// <param name="id"></param>
    public void RemoveSceneById(int id)
    {
        var scene = GetSceneById(id);
        if (scene != null)
        {
            RemoveScene(scene);
        }
    }

    /// <summary>
    /// Add a scene to this collection with no other side effects
    /// </summary>
    /// <param name="scene"></param>
    public override void Add(Scene scene)
    {
        base.Add(scene);
        observers.ForEach(o => o.SceneAdded(scene));
    }

    /// <summary>
    /// Not supported: Insert a scene to this collection with no other side effects
    /// </summary>
    /// <param name="scene"></param>
    public override void Insert(int seq, Scene scene)
    {
        throw new NotImplementedException("Inserting a scene in the Scenes collection is not supported");
    }

    /// <summary>
    /// Remove a scene to this collection with no other side effects
    /// </summary>
    /// <param name="scene"></param>
    public override bool Remove(Scene scene)
    {
        if (base.Remove(scene))
        {
            observers.ForEach(o => o.SceneRemoved(scene));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Remove all scene members for a given device
    /// </summary>
    /// <param name="deviceId">device to remove</param>
    public void RemoveDevice(InsteonID deviceId)
    {
        foreach (Scene scene in this)
        {
            List<SceneMember> membersToRemove = new List<SceneMember>();
            foreach (SceneMember member in scene.Members)
            {
                if (member.DeviceId == deviceId)
                {
                    membersToRemove.Add(member);
                }
            }

            foreach(var member in membersToRemove)
            {
                // No need to remove the links as we are removing the device entirely
                // and that will remove all the links to that device.
                scene.RemoveMember(member, removeLinks: false);
            }
        }
    }

    /// <summary>
    /// Replace a device by another one in all scene members
    /// </summary>
    /// <param name="deviceId">device to replace</param>
    /// <param name="replacementDeviceId">replacement device</param>
    public void ReplaceDevice(InsteonID deviceId, InsteonID replacementDeviceId)
    {
        foreach (Scene scene in this)
        {
            SceneMembers newSceneMembers = new SceneMembers(scene);
            foreach (SceneMember member in scene.Members)
            {
                newSceneMembers.Add(member.DeviceId == deviceId ? 
                    new SceneMember(member) { DeviceId = replacementDeviceId } : member);
            }
            scene.Members = newSceneMembers;
        }
    }

    /// <summary>
    /// Replicate the scene memberships of a given device to another device
    /// </summary>
    /// <param name="deviceId">device to replicate memberships of</param>
    /// <param name="copyDeviceId">device to replicate memberships for</param>
    public void CopyDevice(InsteonID deviceId, InsteonID copyDeviceId)
    {
        foreach (Scene scene in this)
        {
            List<SceneMember> newSceneMembers = new List<SceneMember>();
            foreach (SceneMember member in scene.Members)
            {
                if (member.DeviceId == deviceId)
                {
                    newSceneMembers.Add(new SceneMember(member) { DeviceId = copyDeviceId });
                }
            }
            foreach (var member in newSceneMembers)
            {
                scene.AddMember(member);
            }
        }
    }
}


