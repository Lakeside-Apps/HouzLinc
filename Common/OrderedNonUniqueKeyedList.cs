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
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Common;

/// <summary>
/// A class holding an ordered collection of entries indexed by an int key
/// obtainable from each entry using GetHasCode().
/// Multiple entries can have the same key
/// </summary>
/// <typeparam name="EntryType"></typeparam>
public abstract class OrderedNonUniqueKeyedList<EntryType> : ObservableCollection<EntryType> where EntryType : notnull
{
    // From entry key code to index in List _entries
    Dictionary<int, List<EntryType>> _keyIndex;

    public OrderedNonUniqueKeyedList()
    {
        _keyIndex = new();
    }

    //
    // Summary:
    //     Removes all elements from the System.Collections.ObjectModel.Collection`1.
    protected override void ClearItems()
    {
        _keyIndex.Clear();
        base.ClearItems();
    }

    //
    // Summary:
    //     Replaces the element at the specified index.
    //
    // Parameters:
    //   index:
    //     The zero-based index of the element to replace.
    //
    //   item:
    //     The new value for the element at the specified index. The value can be null for
    //     reference types.
    //
    // Exceptions:
    //   T:System.ArgumentOutOfRangeException:
    //     index is less than zero.-or-index is greater than System.Collections.ObjectModel.Collection`1.Count.
    protected override void SetItem(int index, EntryType entry)
    {
        RemoveFromIndex(this[index]);
        base.SetItem(index, entry);
        AddToIndex(entry);
    }

    //
    // Summary:
    //     Inserts an element into the System.Collections.ObjectModel.Collection`1 at the
    //     specified index.
    //
    // Parameters:
    //   index:
    //     The zero-based index at which item should be inserted.
    //
    //   item:
    //     The object to insert. The value can be null for reference types.
    //
    // Exceptions:
    //   T:System.ArgumentOutOfRangeException:
    //     index is less than zero.-or-index is greater than System.Collections.ObjectModel.Collection`1.Count.
    protected override void InsertItem(int index, EntryType entry)
    {
        // First add to the index because that can fail and throw
        AddToIndex(entry);

        // Then insert into the list and remove from the index if that fails
        try
        {
            base.InsertItem(index, entry);
        }
        catch (Exception)
        {
            RemoveFromIndex(entry);
            throw;
        }
    }

    //
    // Summary:
    //     Removes the element at the specified index of the System.Collections.ObjectModel.Collection`1.
    //
    // Parameters:
    //   index:
    //     The zero-based index of the element to remove.
    //
    // Exceptions:
    //   T:System.ArgumentOutOfRangeException:
    //     index is less than zero.-or-index is equal to or greater than System.Collections.ObjectModel.Collection`1.Count.
    protected override void RemoveItem(int index)
    {
        RemoveFromIndex(this[index]);
        base.RemoveItem(index);
    }

    /// <summary>
    /// Returns whether the list contains an entry matching a given entry
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    public new bool Contains(EntryType entry)
    {
        return TryGetEntry(entry, null, out _);
    }

    /// <summary>
    /// Returns whether the list contains an entry matching a given entry
    /// using a custom comparison
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="comparer"></param>
    /// <returns></returns>
    public bool Contains(EntryType entry, IEqualityComparer<EntryType> comparer)
    {
        return TryGetEntry(entry, comparer, out _);
    }

    /// <summary>
    /// Returns whether the list contains an entry matching a given key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool Contains(int key)
    {
        return _keyIndex.ContainsKey(key);
    }

    /// <summary>
    /// Get the index of the first entry in the list equal to the passed entry using the supplied comparer
    /// Search from the end of the list if searchInReverse is true
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="comparer"></param>
    /// <param name="searchInReverse"></param>"
    /// <returns>index of the intry in this list</returns>
    public int IndexOf(EntryType entry, IEqualityComparer<EntryType>? comparer = null, bool searchInReverse = false)
    {
        comparer ??= EqualityComparer<EntryType>.Default;
        if (searchInReverse)
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                if (comparer.Equals(this[i], entry))
                {
                    return i;
                }
            }
        }
        else
        {
            for (int i = 0; i < Count; i++)
            {
                if (comparer.Equals(this[i], entry))
                {
                    return i;
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Get the first entry in the list equal to the passed entry using the optionally supplied comparer
    /// If no comparer is supplied, this method uses the equality method on the EntryType object
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="matchingEntryInList"></param>
    /// <returns></returns>
    public bool TryGetEntry(EntryType entry, IEqualityComparer<EntryType>? comparer, [NotNullWhen(true)] out EntryType? matchingEntryInList)
    {
        int key = entry.GetHashCode();

        if (TryGetMatchingEntries(key, out List<EntryType>? keyMatchingEntries))
        {
            foreach (EntryType keyMatchingEntry in keyMatchingEntries)
            {
                if ((comparer != null) ? comparer.Equals(keyMatchingEntry, entry) : keyMatchingEntry.Equals(entry))
                {
                    matchingEntryInList = keyMatchingEntry;
                    return true;
                }
            }
        }

        matchingEntryInList = default;
        return false;
    }

    /// <summary>
    /// Get the list of entries matching the passed entry using the optionally supplied comparer
    /// If no comparer is supplied, this method uses the equality method on the EntryType object
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="matchingEntries"></param>
    /// <returns>List of matching entries</returns>
    public bool TryGetMatchingEntries(EntryType entry, IEqualityComparer<EntryType> comparer, [NotNullWhen(true)] out List<EntryType>? matchingEntries)
    {
        int key = entry.GetHashCode();
        matchingEntries = null;

        if (TryGetMatchingEntries(key, out List<EntryType>? keyMatchingEntries) && keyMatchingEntries != null)
        {
            foreach (EntryType keyMatchingEntry in keyMatchingEntries)
            {
                if ((comparer != null) ? comparer.Equals(keyMatchingEntry, entry) : keyMatchingEntry.Equals(entry))
                {
                    matchingEntries ??= new List<EntryType>();
                    matchingEntries.Add(keyMatchingEntry);
                }
            }
        }

        return matchingEntries != null;
    }

    /// <summary>
    /// Returns the list of entries matching a given key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="matchingEntries"></param>
    /// <returns></returns>
    public bool TryGetMatchingEntries(int key, [NotNullWhen(true)] out List<EntryType>? matchingEntries)
    {
        matchingEntries = null;
        if (_keyIndex.TryGetValue(key, out List<EntryType>? matchingValues))
        {
            // We clone the returned list to get a stable list that won't change if changes are made to this OrderedNonUniqueKeyedList
            matchingEntries = new List<EntryType>(matchingValues);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Remove entry matching (equal to) given entry, optionally using a given comparer
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    public bool RemoveMatchingEntry(EntryType entry, IEqualityComparer<EntryType>? comparer = null)
    {
        var index = IndexOf(entry, comparer);
        if (index != -1)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    // Remove an given entry from the index
    // Returns true if entry was removed
    private bool RemoveFromIndex(EntryType entry)
    {
        bool success = false;

        if (entry != null)
        {
            int key = entry.GetHashCode();

            if (_keyIndex.TryGetValue(key, out List<EntryType>? matchingEntries))
            {
                for (int i = 0; i < matchingEntries.Count;)
                {
                    if (Object.ReferenceEquals(matchingEntries[i], entry))
                    {
                        matchingEntries.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                if (matchingEntries.Count == 0)
                {
                    _keyIndex.Remove(key);
                    matchingEntries = null;
                }

                success = true;
            }

            Debug.Assert(matchingEntries == null || matchingEntries.Count > 0);
        }

        return success;
    }

    // Add an entry to the index
    private void AddToIndex(EntryType entry)
    {
        if (entry != null)
        {
            int key = entry.GetHashCode();
            if (!_keyIndex.TryGetValue(key, out List<EntryType>? keyMatchingEntries))
            {
                keyMatchingEntries = new List<EntryType>();
                _keyIndex.Add(key, keyMatchingEntries);
            }
            keyMatchingEntries.Add(entry);

            Debug.Assert(keyMatchingEntries != null);
            Debug.Assert(keyMatchingEntries.Count > 0);
        }
    }

    /// <summary>
    /// Remove duplicate entries in the list.
    /// Optionally delegate the actual removal to a passed-in callback.
    /// If not callback is passed, Remove() is used.
    /// </summary>
    public void RemoveDuplicateEntries(Action<EntryType>? RemoveEntryCallback = null)
    {
        List<EntryType> duplicateEntries = new List<EntryType>();
        HashSet<EntryType> seenEntries = new HashSet<EntryType>();

        foreach (var entry in this)
        {
            if (!seenEntries.Add(entry))
            {
                duplicateEntries.Add(entry);
            }
        }

        foreach (EntryType entry in duplicateEntries)
        {
            if (RemoveEntryCallback != null)
                RemoveEntryCallback.Invoke(entry);
            else
                Remove(entry);
        }
    }
}
