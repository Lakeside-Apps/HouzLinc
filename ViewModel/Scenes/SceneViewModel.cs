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
using ViewModel.Base;
using System.ComponentModel;
using Common;

namespace ViewModel.Scenes;

[Bindable(true)]
public sealed class SceneViewModel : ItemViewModel, ISceneObserver, IRoomsObserver
{
    private SceneViewModel(Scene scene)
    {
        this.Scene = scene;
    }

    /// <summary>
    /// Get or create a view model for the given scene
    /// Garantees that only one view model will be create for a given scene.
    /// </summary>
    /// <param name="scene">the scene to get the view model for</param>
    /// <returns>the created scene view model</returns>
    public static SceneViewModel GetOrCreate(Scene scene)
    {
        sceneViewModels ??= new Dictionary<int, SceneViewModel>();

        if (!sceneViewModels.TryGetValue(scene.Id, out SceneViewModel? sceneViewModel))
        {
            sceneViewModel = new SceneViewModel(scene);
            sceneViewModels.Add(scene.Id, sceneViewModel);
            scene.AddObserver(sceneViewModel);
        }

        return sceneViewModel;
    }

    /// <summary>
    /// Get or create a view model for the given scene key
    /// Garantees that only one view model will be create for a given scene.
    /// </summary>
    /// <param name="itemKey"></param>
    /// <returns>Found SceneViewModel or null if none exists with this itemKey</returns>
    public static SceneViewModel? GetOrCreateItemByKey(House house, string itemKey)
    {
        int sceneId = int.Parse(itemKey);
        var scene = house.Scenes.GetSceneById(sceneId);
        return scene != null ? SceneViewModel.GetOrCreate(scene) : null;
    }

    // List of already created scene view models
    // TODO: multiple houses - the key in this dictionary needs to differenciate on the house or hub
    // if we ever want to support multiple houses concurrently
    private static Dictionary<int, SceneViewModel>? sceneViewModels;

    // Scene associated to this view model
    public Scene Scene { get; private set; }

    /// <summary>
    /// Notifies this scene view model that it is made active
    /// (presented on screen) or inactive (hidden from screen)
    /// </summary>
    public override void ActiveStateChanged()
    {
        base.ActiveStateChanged();
        OnPropertyChanged(nameof(IsOnOffButtonShown));

        if (IsActive)
        {
            Scene.House.Rooms.AddObserver(this);
        }
        else
        {
            Scene.House.Rooms.RemoveObserver(this);
        }
    }

    /// <summary>
    /// Scene id and key
    /// </summary>
    public string DisplayId => Id.ToString();

    public int Id => Scene.Id;
    public override string ItemKey  => Id.ToString();

    /// <summary>
    /// Scene name as displayed in the UI
    /// </summary>
    public string DisplayName
    {
        get => Scene.Name;
        set
        {
            if (value != Scene.Name)
            {
                Scene.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public string DisplayNameAndId => this.DisplayName + " (" + this.Id + ")";

    /// <summary>
    /// Scene room
    /// </summary>
    public string Room
    {
        get => Scene.Room ?? "";
        set
        {
            if (value != Scene.Room)
            {
                Scene.Room = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Scene notes
    /// </summary>
    public string Notes
    {
        get => Scene.Notes ?? string.Empty;
        set
        {
            if (value != Scene.Notes)
            {
                Scene.Notes = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Indicate wherer the Scene Name can be edited in the UI at the moment
    /// </summary>
    public bool IsNameEditable
    {
        get => isNameEditable;
        set
        {
            if (value != isNameEditable)
            {
                isNameEditable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNoteReadOnlyWithNoText));
            }
        }
    }
    bool isNameEditable;

    /// <summary>
    /// Note is read-only (not editable) and empty
    /// </summary>
    public bool IsNoteReadOnlyWithNoText => !IsNameEditable && Notes.Length == 0;


    /// <summary>
    /// Are scene properties synced
    /// Read only, one way bindable
    /// </summary>
    public bool IsSceneSynced
    {
        get => false;
    }
    public bool IsSceneSyncNeeded
    {
        get => !IsSceneSynced;
    }

    /// <summary>
    /// Handler for the "Expand Scene" menu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ExpandScene_Click(Object sender, RoutedEventArgs e)
    {
        Scene.Expand();
    }

    /// <summary>
    /// Remove stale scene members, i.e., where the device is unknown
    /// UI menu handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void RemoveStaleMembers_Click(Object sender, RoutedEventArgs e)
    {
        Scene.RemoveStaleMembers();

    }

    /// <summary>
    /// Remove duplicate scene members
    /// UI menu handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void RemoveDuplicateMembers_Click(object sender, RoutedEventArgs e)
    {
        Scene.RemoveDuplicateMembers();
    }

    /// <summary>
    /// Does this scene have any member
    /// </summary>
    public bool HasAnyMember => Scene.Members.Count > 0;

    /// <summary>
    /// List of members view models for this scene
    /// </summary>
    public MemberListViewModel SceneMembers
    {
        get
        {
            if (this.sceneMembers == null)
            {
                this.sceneMembers = new MemberListViewModel(this);

                foreach (SceneMember member in this.Scene.Members)
                {
                    sceneMembers.Add(new MemberViewModel(sceneMembers, member));
                }
            }
            return this.sceneMembers;
        }
    }
    private MemberListViewModel? sceneMembers;

    /// <summary>
    /// Remove this scene
    /// </summary>
    public void RemoveScene()
    {
        Scene.House.Scenes.RemoveSceneById(Id);
    }

    // Observer methods
    // Handle change notification from the model

    void ISceneObserver.ScenePropertyChanged(Scene scene, string? propertyName)
    {
        switch(propertyName)
        {
            case "Name":
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(DisplayName));
                break;
            default:
                OnPropertyChanged(propertyName);
                break;
        }
    }

    void ISceneObserver.SceneMembersChanged(Scene? scene, SceneMembers members)
    {
        // Force regeneration of the scene members view model
        sceneMembers = null;

        // Notifies the UI of property changing when members change
        OnPropertyChanged(nameof(IsOnOffButtonShown));
    }

    /// <summary>
    /// Turn on the responders on this channel
    /// For now, only full level is supported
    /// </summary>
    public void SceneOn()
    {
        channel?.ScheduleTurnOn(1.0f);
    }

    /// <summary>
    /// Turn on the responders on this channel
    /// </summary>
    public void SceneFullOn()
    {
        channel?.ScheduleTurnOn(1.0f);
    }

    /// <summary>
    /// Turn off the responders on this channel
    /// </summary>
    public void SceneOff()
    {
        channel?.ScheduleTurnOff();
    }

    // Helper to get the hub channel that controls this scene, if any
    private Channel? channel => hubControllerMember != null ? Scene.House.Hub?.Channels[hubControllerMember.Group] ?? null : null;

    // Helper to get the hub member that controls this scene, if any
    private SceneMember? hubControllerMember => Scene.Members.FirstOrDefault(
        m => m.DeviceId == (Scene.House.Hub?.Id ?? InsteonID.Null) &&
                m.IsController == true &&
                m.Group != 0);

    // Helper to determine whether this scene has a hub as a controller member
    private bool HasHubAsControllerMember => hubControllerMember != null;

    /// <summary>
    /// Should the On/Off button be shown in the list of channels
    /// </summary>
    public bool IsOnOffButtonShown
    {
#if ANDROID || IOS
        get => HasHubAsControllerMember;
#else
        get => (IsActive || IsPointerOver) && HasHubAsControllerMember;
#endif
    }

    // Show the On/Off button when the mouse is over the channel
    public override void PointerOverChanged()
    {
        OnPropertyChanged(nameof(IsOnOffButtonShown));
    }

    /// <summary>
    /// Bindable - List of rooms to bind the RoomControl to
    /// </summary>
    /// <returns></returns>
    public SortableObservableCollection<string> Rooms
    {
        get
        {
            if (rooms == null)
            {
                rooms = new SortableObservableCollection<string>(Scene.House.Rooms);
                // TODO: localize
                rooms.Insert(0, "None");
            }
            return rooms;
        }
    }

    // TODO: static reduces the number of rebuilds of that list 
    // but will not work if we have multiple houses in future.
    private SortableObservableCollection<string>? rooms;

    /// <summary>
    /// Notification handler that the rooms list may have changed
    /// </summary>
    public void RoomsChanged()
    {
        rooms = null;
        OnPropertyChanged(nameof(Rooms));
    }
}
