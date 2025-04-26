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

using System.Collections.ObjectModel;

namespace ViewModel.Base;

public enum SortDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Adds ability to sort the list to an ObservableCollection
/// </summary>
/// <typeparam name="T"></typeparam>
public class SortableObservableCollection<T> : ObservableCollection<T>
{
    public SortableObservableCollection() {}

    public SortableObservableCollection(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            Add(item);
        }
    }

    /// <summary>
    /// Sort this list
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public void Sort<TKey>(Func<T, TKey> keySelector, SortDirection direction)
    {
        switch (direction)
        {
            case SortDirection.Ascending:
                {
                    Replace(Items.OrderBy(keySelector));
                    break;
                }
            case SortDirection.Descending:
                {
                    Replace(Items.OrderByDescending(keySelector));
                    break;
                }
        }
    }

    /// <summary>
    /// Sort this list, using a IComparer
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="keySelector"></param>
    /// <param name="direction"></param>
    /// <param name="comparer"></param>
    public void Sort<TKey>(Func<T, TKey> keySelector, SortDirection direction, IComparer<TKey> comparer)
    {
        switch (direction)
        {
            case SortDirection.Ascending:
                {
                    Replace(Items.OrderBy(keySelector, comparer));
                    break;
                }
            case SortDirection.Descending:
                {
                    Replace(Items.OrderByDescending(keySelector, comparer));
                    break;
                }
        }
    }

    /// <summary>
    /// Filter this list based on a predicate
    /// </summary>
    /// <param name="predicate"></param>
    public void Filter(Func<T, bool> predicate)
    {
        var newItems = Items.Where(predicate);
        Replace(newItems);
    }

    /// <summary>
    /// Replace all the items in this list by the items of another list
    /// </summary>
    /// <param name="newItems"></param>
    public void Replace(IEnumerable<T> newItems)
    {
        // First copy the newItems list as LINQ queries are deferred and
        // we need to instanciate it before we start modifying this list,
        // from which newItems is derived
        List<T> list = [.. newItems];

        // Then replace items in the this list to make all changes observable
        // and progressive in the UX
        int i = 0;
        foreach (var item in list)
        {
            if (i < Count)
                SetItem(i, item);
            else
                Add(item);
            i++;
        }

        while (i < Count)
        {
            RemoveAt(i);
        }
    }
}
