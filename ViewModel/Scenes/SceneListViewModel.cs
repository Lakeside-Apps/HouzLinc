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

using System.Diagnostics;
using Insteon.Model;
using ViewModel.Base;
using ViewModel.Settings;

namespace ViewModel.Scenes;

public class SceneListViewModel : ItemListViewModel<SceneViewModel>, IScenesObserver, IRoomsObserver
{
    // A public default constructor is necessary to make the generated binding code compile
    // but it should never be called as we always instantiate it with a list of scenes.
    public SceneListViewModel()
    {
        Debug.Assert(false, "SceneListViewModel should always be created with a list of scenes.");
        this.scenes = null!;
    }

    private SceneListViewModel(Insteon.Model.Scenes scenes)
    {
        this.scenes = scenes;
        HasNoScene = scenes.Count == 0;
        RebuildList();
    }

    public static SceneListViewModel Create(Insteon.Model.Scenes scenes)
    {
        return new SceneListViewModel(scenes);
    }

    public SceneListViewModel ApplyFilterAndSortOrderFromSettings()
    {
        // Retrieve last used sort order
        SortOrder = SettingsStore.ReadLastUsedValueAsString("ScenesSortOrder") ?? string.Empty;

        // Retrieve last used room filter
        var roomFilter = SettingsStore.ReadLastUsedValueAsString("ScenesRoomFilter") ?? string.Empty;
        var rooms = Rooms;
        if (!rooms.Contains(roomFilter))
        {
            roomFilter = rooms.Count > 0 ? rooms[0] : string.Empty;
        }
        RoomFilter = roomFilter;
        return this;
    }

    // To identify items in the SettingsStore
    protected override string ItemTypeName => "Scene";

    /// <summary>
    /// One-way bindable property 
    /// Whether the underlying scene collection is empty
    /// </summary>
    public bool HasNoScene
    {
        get => hasNoScene;
        set
        {
            if (value != hasNoScene)
            {
                hasNoScene = value;
                OnPropertyChanged();
            }
        }
    }
    private bool hasNoScene;

    /// <summary>
    /// The page using this has loaded
    /// </summary>
    public override void ViewLoaded()
    {
        scenes.AddObserver(this);
        scenes.House.Rooms.AddObserver(this);
    }

    /// <summary>
    /// The page using this has unloaded
    /// </summary>
    public override void ViewUnloaded()
    {
        scenes.RemoveObserver(this);
        scenes.House.Rooms.RemoveObserver(this);
    }

    /// <summary>
    /// Builds this list of DeviceViewModels from the model
    /// </summary>
    public void RebuildList()
    {
        Items.Clear();

        // Build the list of DeviceViewModels 
        foreach (Scene s in scenes)
        {
            // The hub does not appear in the list
            Items.Add(SceneViewModel.GetOrCreate(s));
        }
    }

    /// <summary>
    /// Get a SceneViewModel object from this list by Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    internal SceneViewModel? GetSceneViewModelById(int id)
    {
        foreach (SceneViewModel svm in Items)
        {
            if (svm.Id == id)
            {
                return svm;
            }
        }
        return null;
    }

    /// <summary>
    /// Add a new scene and sceneViewModel to this list
    /// Notification coming through the observer will add the sceneViewModel to the list
    /// </summary>
    /// <param name="name"></param>
    public void AddNewScene(string name)
    {
        Scene scene = scenes.AddNewScene(name);
    }

    /// <summary>
    /// Delete scene by Id from this list
    /// Notification coming through the observer will remove the sceneViewModel from the list
    /// </summary>
    /// <param name="id"></param>
    public void RemoveScene(int id)
    {
        var scene = scenes.GetSceneById(id);
        if (scene != null)
        {
            scenes.RemoveScene(scene);
        }
    }

    /// <summary>
    /// Expand all scenes, creating appropriate links for each scene
    /// </summary>
    public void ExpandAllScenes()
    {
        foreach(Scene scene in scenes)
        {
            scene.Expand();
        }
    }

    /// <summary>
    /// Bindable - Returns the list of sort orders
    /// Bindable to the UI
    /// </summary>
    public List<string> SortOrders
    {
        get
        {
            if (sortOrders == null)
            {
                sortOrders = new List<string>();
                sortOrders.Add("");
                sortOrders.Add("Id");
                sortOrders.Add("Name");
                sortOrders.Add("Room");
            }
            return sortOrders;
        }
    }
    private List<string>? sortOrders;

    /// <summary>
    /// Bindable - Scene list sort order
    /// </summary>
    public string SortOrder
    {
        get => sortOrder;
        set
        {
            if (value != sortOrder)
            {
                sortOrder = value;
                OnPropertyChanged();
                ApplySortOrder();
                SettingsStore.WriteLastUsedValue("ScenesSortOrder", sortOrder);
            }
        }
    }
    private string sortOrder = string.Empty;

    // Apply current sort order to this list
    private void ApplySortOrder()
    {
        // Remember the key of the selected item, if any
        var selectedItemKey = SelectedItem?.ItemKey.ToString();

        switch (sortOrder)
        {
            case "Id":
                SortById(SortDirection.Ascending);
                break;
            case "Name":
                SortByName(SortDirection.Ascending);
                break;
            case "Room":
                SortByRoom(SortDirection.Ascending);
                break;
            default:
                // go back to original order and repeat the room filter
                ApplyRoomFilter();
                break;
        }

        // Attempt to reselect the same item
        if (selectedItemKey != null)
            TrySelectItemByKey(selectedItemKey);
    }

    /// <summary>
    /// Bindable - List of rooms devices are in
    /// </summary>
    /// <returns></returns>
    public List<string> Rooms
    {
        get
        {
            if (rooms == null)
            {
                rooms = new List<string>(scenes.House.Rooms);
                // TODO: localize
                rooms.Insert(0, "Any");
                rooms.Insert(1, "None");
            }
            return rooms;
        }
    }
    private List<string>? rooms;

    /// <summary>
    /// Bindable - Current room filter
    /// </summary>
    public string RoomFilter
    {
        get => roomFilter;
        set
        {
            if (value != roomFilter)
            {
                roomFilter = value;
                OnPropertyChanged();
                ApplyRoomFilter();
                ApplySortOrder();
                SettingsStore.WriteLastUsedValue("ScenesRoomFilter", roomFilter);
            }
        }
    }
    private string roomFilter = string.Empty;

    // Apply the current room filter to the list
    private void ApplyRoomFilter()
    {
        // Remember the key of the selected item, if any
        var selectedItemKey = SelectedItem?.ItemKey.ToString();

        // Reset and reapply filter
        RebuildList();
        FilterByRoom(RoomFilter);

        // If the selection was set before, attempt to reselect the same item,
        // or if it is not in the list anymore, default to the first item
        if (Items.Count > 0 && selectedItemKey != null && !TrySelectItemByKey(selectedItemKey))
            SelectedItem = Items.First();
    }

    /// <summary>
    /// Sort the list by different keys
    /// </summary>
    /// <param name="direction"></param>
    public void SortById(SortDirection direction)
    {
        Items.Sort<int>(x => x.Id, direction);
    }

    public void SortByName(SortDirection direction)
    {
        Items.Sort<string>(x => x.DisplayName, direction);
    }

    public void SortByRoom(SortDirection direction)
    {
        Items.Sort<string>(x => x.Room, direction);
    }

    public void FilterByRoom(string room)
    {
        if (room == string.Empty || room == "Any")
        {
            return;
        }

        if (room == "None")
        {
            Items.Filter(x => x.Room == "" || x.Room == null || x.Room == "None");
            return;
        }

        Items.Filter(x => x.Room == room);
    }

    // Implementation of IScenesObserver
    // Update the view model on change notifications from the model,
    // which will update the UI via data binding.

    void IScenesObserver.SceneAdded(Scene scene)
    {
        SceneViewModel sceneViewModel = SceneViewModel.GetOrCreate(scene);
        Items.Add(sceneViewModel);
        RoomsChanged();
    }

    void IScenesObserver.SceneRemoved(Scene scene)
    {
        var index = -GetItemIndexByKey(scene.Id.ToString());
        if (index != -1)
        {
            Items.RemoveAt(index);
            RoomsChanged();
        }
    }

    void IScenesObserver.ScenesPropertyChanged(Insteon.Model.Scenes scenes)
    {
        // None of the properties affect the UI
    }

    /// <summary>
    /// Notification handler that the list may have changed
    /// </summary>
    public void RoomsChanged()
    {
        rooms = null;
        OnPropertyChanged(nameof(Rooms));
    }

    private Insteon.Model.Scenes scenes;
}
