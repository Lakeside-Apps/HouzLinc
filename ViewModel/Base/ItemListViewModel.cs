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

using ViewModel.Settings;

namespace ViewModel.Base;

public abstract class ItemListViewModel<ItemViewModelType> : PageViewModel where ItemViewModelType : ItemViewModel
{
    /// <summary>
    /// Item collection
    /// </summary>
    public SortableObservableCollection<ItemViewModelType> Items
    {
        get
        {
            items ??= new();
            items.CollectionChanged += (sender, e) =>
            {
                IsViewEmpty = items.Count == 0;
            };
            return items;
        }
    }
    private SortableObservableCollection<ItemViewModelType>? items;

    /// <summary>
    /// One-way UI bindable property 
    /// Whether this view is empty, either because there are no item in the model or none matches the filtering
    /// </summary>
    public bool IsViewEmpty
    {
        get => isViewEmpty;
        set
        {
            if (value != isViewEmpty)
            {
                isViewEmpty = value;
                OnPropertyChanged();
            }
        }
    }
    private bool isViewEmpty;

    /// <summary>
    /// Called when the page (view) had loaded
    /// </summary>
    public override void ViewLoaded()
    {
    }

    /// <summary>
    /// Called when the page (view) is unloaded
    /// </summary>
    public override void ViewUnloaded()
    {
        SelectedItem = null;
    }

    /// <summary>
    /// Get item index in the Items list by item key, -1 if not found
    /// </summary>
    /// <param name="itemKey"></param>
    /// <returns>item index in the Items list, -1 if not found</returns>
    public int GetItemIndexByKey(string itemKey)
    {
        int index = 0;
        foreach (var item in Items)
        {
            if (item.ItemKey == itemKey)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    /// <summary>
    /// Currently selected item, presented in the item details panel
    /// </summary>
    public ItemViewModelType? SelectedItem
    {
        get => selectedItem;
        set
        {
            if (value != selectedItem)
            {
                var oldItem = selectedItem;
                selectedItem = value;

                OnSelectedItemChanged(oldItem, selectedItem);
                OnPropertyChanged();
                OnPropertyChanged(nameof(PresentedItem));
                OnPropertyChanged(nameof(IsItemPresented));

                // Store new selection in SettingsStore to reuse after navigation away and back or in 
                // next session. Note that we would like to do this only in the Unload event handler,
                // but that event does not fire on shutdown.
                if (selectedItem != null)
                {
                    WriteLastSelectedItemToSettingsStore(selectedItem);
                }
            }
        }
    }
    private ItemViewModelType? selectedItem;

    /// <summary>
    /// Returns the key of the last selected item from the SettingsStore
    /// </summary>
    /// <returns></returns>
    public string? ReadLastSelectedItemFromSettingsStore()
    {
        return SettingsStore.ReadLastUsedValueAsString($"Selected{ItemTypeName}");
    }

    /// <summary>
    /// Write the key of the given item as last selected in the SettingsStore
    /// </summary>
    /// <param name="selectedItem"></param>
    public void WriteLastSelectedItemToSettingsStore(ItemViewModelType selectedItem)
    {
        SettingsStore.WriteLastUsedValue($"Selected{ItemTypeName}", selectedItem.ItemKey);
    }

    // Item type readable name for use in SettingsStore
    // Provided by derived classes
    protected abstract string ItemTypeName { get; }

    /// <summary>
    /// Called when the page size visual state group changes
    /// <param name="isNarrow"> true if the new state is narrow, false if wide</param>
    /// </summary>
    public void PageSizeStateChanged(bool isNarrow)
    {
        IsNarrowPresentation = isNarrow;
        OnPropertyChanged(nameof(PresentedItem));
        OnPropertyChanged(nameof(IsItemPresented));
    }
    bool IsNarrowPresentation;

    /// <summary>
    /// Item to present the detail of, either the SelectedItem or null in narrow mode when the
    /// ItemDetailPresented is not shown.
    /// </summary>
    // TODO: Uno Desktop calls this with CurrentStatus == null in some cases
    public ItemViewModelType? PresentedItem => IsNarrowPresentation ? null : SelectedItem;

    /// <summary>
    /// Bindable property to indicate whether an item is currently presented or not
    /// </summary>
    public bool IsItemPresented => PresentedItem != null;

    /// <summary>
    /// Notifies derived classes that old item was deselected, and new item was selected
    /// </summary>
    /// <param name="oldItem"></param>
    /// <param name="newItem"></param>
    public virtual void OnSelectedItemChanged(ItemViewModelType? oldItem, ItemViewModelType? newItem)
    {
        if (oldItem != null)
        {
            // Deactive currently active item view model if we are in wide state.
            // In narrow state, the item will be deactivated when we navigate away from the detail page.
            if (!IsNarrowPresentation)
            {
                oldItem.IsActive = false;
            }
        }

        if (newItem != null)
        {
            // Active currently item view model if we are in wide state.
            // In narrow state, the item will be activated when we navigate to the detail page.
            if (!IsNarrowPresentation)
            {
                newItem.IsActive = true;
            }
        }
    }

    /// <summary>
    /// Set currently selected item by item key
    /// itemKey is a string that derived classes understand
    /// If the item is not found, no item is selected, and false is returned
    /// </summary>
    /// <param name="itemKey"></param>
    /// <returns>true if the item existed and was selected</returns>
    public bool TrySelectItemByKey(string itemKey)
    {
        if (itemKey != null)
        {
            try
            {
                var item = Items.FirstOrDefault((item) => item!.ItemKey == itemKey, null);
                if (item != null)
                {
                    SelectedItem = item;
                    ItemSelected?.Invoke(item);
                    return true;
                }
            }
            catch { }
        }
        return false;
    }

    public event Func<ItemViewModelType, Task>? ItemSelected;
}
