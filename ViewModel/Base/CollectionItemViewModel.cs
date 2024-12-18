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

namespace ViewModel.Base;

public class CollectionItemViewModel<CollectionViewModelType, ItemViewModelType> : BaseViewModel
{
    internal CollectionItemViewModel(CollectionViewModelType containingList)
    {
        this.containingList = containingList;
    }

    internal CollectionItemViewModel(CollectionItemViewModel<CollectionViewModelType, ItemViewModelType> other)
    {
        this.containingList = other.containingList;
    }

    // Containing LinkListViewModel
    protected CollectionViewModelType containingList;

    /// <summary>
    /// Whether this link is selected in the list
    /// Bindable, one-way
    /// </summary>
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected != value)
            {
                isSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEditButtonShown));
            }
        }
    }
    private bool isSelected;

    /// <summary>
    /// Should Edit/Remove button shown for this item.
    /// Selected or mouseovered on Windows, all items on mobile.
    /// </summary>
#if ANDROID || IOS
    public bool IsEditButtonShown => true;
#else
    public bool IsEditButtonShown => IsSelected || IsPointerOver;
#endif
}
