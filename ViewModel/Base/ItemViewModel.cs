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

/// <summary>
/// Base class for an "item" view model, item being a Device, Hub Channel, Link, or Scene
/// Defines the common interface for all item view models
/// </summary>
public class ItemViewModel : BaseViewModel
{
    // Equality between ItemViewModels is based on ItemKey
    public override bool Equals(object? otherItem)
    {
        if (otherItem is ItemViewModel otherItemViewModel)
        {
            return ItemKey == otherItemViewModel.ItemKey;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    /// <summary>
    /// Key used to identify items in ItemViewModel lists
    /// </summary>
    public virtual string ItemKey => "";

    /// <summary>
    /// This is called when navigating to the item list or detail page and should be overridden by derived classes
    /// wanting to handle the navigation parameter (e.g., present a given channel on a device with channels)
    /// </summary>
    /// <param name="parameter"></param>
    public virtual void SetNavigationParameter(string parameter) { }

    /// <summary>
    /// This is called when navigating from the item list or detail page and should be overridden by derived classes
    /// wanting to provide navigation parameter to be retrieved using the method above when navigating back to this item.
    /// </summary>
    /// <returns>The navigation parameter, null if none</returns>
    public virtual string? GetNavigationParameter() { return null; }

    /// <summary>
    /// Notifies this ItemViewModel that it active/unactive (presented on screen or not)
    /// Also called when the connected state changes (IsConnected)
    /// </summary>
    public virtual void ActiveStateChanged() { }

    /// <summary>
    /// Is this ItemViewModel active (presented on screen)
    /// </summary>
    public bool IsActive
    {
        get => isActive;
        set
        {
            if (value != isActive)
            {
                isActive = value;
                ActiveStateChanged();
            }
        }
    }
    private bool isActive;
}
