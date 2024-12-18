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

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Common;

/// <summary>
/// A class holding an ordered list of entries indexed by an int key.
/// The key is obtainable by calling GetHashCode on the entry.
/// Keys must be unique for each entry, use OrderedNonUniqueKeyedList if they are not.
/// 
/// </summary>
public abstract class OrderedKeyedList<EntryType> : ICollection<EntryType> where EntryType: notnull
{
    List<EntryType> _entries;
    Dictionary<int, int> _index;        // entry key to index in List _entries

    public OrderedKeyedList()
    {
        _entries = new();
        _index = new();
    }

    public int Count => _entries.Count();

    public bool IsSynchronized => throw new NotImplementedException();

    public object SyncRoot => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    public EntryType this[int seq]
    {
        get => _entries[seq];
        set
        {
            _index.Remove(_entries[seq].GetHashCode());
            _entries[seq] = value;
            _index.Add(value.GetHashCode(), seq);
        }
    }

    public virtual void Add(EntryType entry)
    {
        // First add the key to the index because this can fail and throw
        _index.Add(entry.GetHashCode(), _entries.Count);

        // Then add to the list and remove the key from the index if that fails
        try
        {
            _entries.Add(entry);
        }
        catch (Exception)
        {
            _index.Remove(entry.GetHashCode());
            throw;
        }
    }

    public virtual void Insert(int seq, EntryType entry)
    {
        // Inserting at the end of the list is the same as adding
        if (seq == _entries.Count)
        {
            Add(entry);
            return;
        }

        // Insert the entry in the list and, if successful rebuild the index
        _entries.Insert(seq, entry);
        RebuildIndex();
    }

    public virtual bool Remove(EntryType entry)
    {
        if (_entries.Remove(entry))
        {
            RebuildIndex();
            return true;
        }
        return false;
    }

    public bool Contains(int key)
    {
        return _index.ContainsKey(key);
    }

    public bool TryGetSequenceNumber(int key, out int seq)
    {
        return _index.TryGetValue(key, out seq);
    }

    public bool TryGetEntry(int key, [NotNullWhen(true)] out EntryType? entry)
    {
        entry = default;
        if (_index.TryGetValue(key, out int seq))
        {
            entry = _entries[seq]!;
            return true;
        }
        return false;
    }

    public void CopyTo(Array array, int index)
    {
        int i = index;
        foreach(EntryType entry in this)
        {
            array.SetValue(entry, i);
        }
    }

    public IEnumerator<EntryType> GetEnumerator()
    {
        return _entries.GetEnumerator();
    }

    private void RebuildIndex()
    {
        _index.Clear();
        int seq = 0;
        foreach (EntryType entry in _entries)
        {
            _index.Add(entry.GetHashCode(), seq);
            seq++;
        }
    }

    // ICollection<EntryType> implementation

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(EntryType item)
    {
        return TryGetEntry(item.GetHashCode(), out _);
    }

    public void CopyTo(EntryType[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    bool ICollection<EntryType>.Remove(EntryType item)
    {
        return Remove(item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_entries).GetEnumerator();
    }
}
