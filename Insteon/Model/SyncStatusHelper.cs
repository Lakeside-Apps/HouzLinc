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

namespace Insteon.Model;

/// <summary>
/// Desribes whether a property, all properties, a channel or a link record of a logical device is synced with the same property or link record in the physical device 
/// This gets persisted from session to session
/// </summary>
public enum SyncStatus
{
    Unknown,            // we have never read this property, channel or link record from the network
    Synced,             // both logical and physical devices have the same value
    Changed,            // logical and physical devices have different values
}

// Persistence helpers
internal static class SyncStatusHelper
{
    internal static string? Serialize(SyncStatus status)
    {
        switch (status)
        {
            case SyncStatus.Synced:
                return "Synchronized";
            case SyncStatus.Changed:
                return "Changed";
            default:
                return null;
        }
    }

    internal static SyncStatus Deserialize(string? value)
    {
        switch (value)
        {
            case "Synchronized":
                return SyncStatus.Synced;
            case "Changed":
                return SyncStatus.Changed;
            default:
                return SyncStatus.Unknown;
        }
    }

    internal static string? SerializeForLink(SyncStatus status)
    {
        switch (status)
        {
            case SyncStatus.Changed:
                return "changed";
            case SyncStatus.Synced:
                return "synced";
            default:
                return null;
        }
    }

    internal static SyncStatus DeserializeForLink(string? value)
    {
        switch (value)
        {
            case "notsynced":
            case "changed":
                return SyncStatus.Changed;
            case "synced":
            case "deleted":
                return SyncStatus.Synced;
            default:
                return SyncStatus.Unknown;
        }
    }
}
