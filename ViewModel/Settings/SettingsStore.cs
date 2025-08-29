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

using System.ComponentModel.DataAnnotations;
using Windows.Storage;
using Common;

namespace ViewModel.Settings;

// Stores and retrieves settings in the registry

public static class SettingsStore
{
    public static object? ReadValue(string name)
    {
        return MainContainer.Values[name] as string;
    }

    public static void WriteValue(string name, object value)
    {
        MainContainer.Values[name] = value;
    }

    public static object? ReadValueFromContainer(string container, string name)
    {
        return GetContainer(container).Values[name];
    }

    public static void WriteValueToContainer(string container, string name, object value)
    {
        GetContainer(container).Values[name] = value;
    }

    public static void RemoveValueFromContainer(string container, string name)
    {
        GetContainer(container).Values.Remove(name);
    }

    // Preferences

    private const string UserPrefContainer = "UserPreferences";

    public static object? ReadUserPrefValue(string name)
    {
        return GetContainer(UserPrefContainer).Values[name];
    }

    public static void WriteUserPrefValue(string name, object value)
    {
        GetContainer(UserPrefContainer).Values[name] = value;
    }

    public static void RemoveUserPrefValue(string name)
    {
        GetContainer(UserPrefContainer).Values.Remove(name);
    }

    // Last used values

    private const string LastUsedContainer = "LastUsed";

    public static object? ReadLastUsedValue(string name)
    {
        return GetContainer(LastUsedContainer).Values[name];
    }

    public static void WriteLastUsedValue(string name, object value)
    {
        Logger.Log.Debug($"SettingsStore: writing last used value for {name}: {value}");
        GetContainer(LastUsedContainer).Values[name] = value;
    }

    public static string? ReadLastUsedValueAsString(string name)
    {
        return GetContainer(LastUsedContainer).Values[name] as string;
    }

    public static void WriteLastUsedValue(string name, string value)
    {
        GetContainer(LastUsedContainer).Values[name] = value;
    }

    public static void RemoveLastUsedValue(string name)
    {
        GetContainer(LastUsedContainer).Values.Remove(name);
    }

    public static int ReadLastUsedValueAsInt(string name, int defaultValue)
    {
        var objectValue = GetContainer(LastUsedContainer).Values[name];
        Logger.Log.Debug($"SettingsStore: reading last used value for {name}: {objectValue}");
        return (objectValue != null) ? (int)objectValue : defaultValue;
    }

    /// <summary>
    /// Read list from the Settings store
    /// </summary>
    /// <param name="name"></param>
    /// <param name="list"></param>
    public static void ReadLastUsedStringList(string name, List<string> list)
    {
        // Retrieve previous list from Settings store
        var arrayValue = SettingsStore.ReadLastUsedValue(name) as string[];
        if (arrayValue != null)
        {
            list.AddRange(arrayValue);
        }
    }

    /// <summary>
    /// Write the last maxItems entries of a list to the Settings store
    /// </summary>
    /// <param name="name"></param>
    /// <param name="list"></param>
    /// <param name="maxItems"></param>
    public static void WriteLastUsedStringList(string name, List<string> list, int maxItems)
    {
        // Store only the last maxItems entries
        var arrayValue = list.Skip(Math.Max(0, list.Count - maxItems)).ToArray();
        WriteLastUsedValue(name, arrayValue);
    }

    public static void Clear()
    {
        MainContainer.Clear();
    }

    private static XPlatApplicationDataContainer MainContainer => mainContainer ??= new XPlatApplicationDataContainer();
    private static XPlatApplicationDataContainer? mainContainer;

    private static XPlatApplicationDataContainer GetContainer(string name)
    {
        return MainContainer.CreateContainer(name, ApplicationDataCreateDisposition.Always);
    }
}

// An extension of ApplicationDataContainer that similuates LocalSettings.Container on non Windows platform where it is not implemented
// On non-windows platform, we use only one container and prefix the key with the container name to avoid collisions
// For unpackaged Windows apps, we use file-based storage when ApplicationDataContainer is not available
internal class XPlatApplicationDataContainer
{
    internal class ValueCollection
    {
        internal ValueCollection(XPlatApplicationDataContainer xPlatContainer)
        {
            this.xPlatContainer = xPlatContainer;
        }

        private XPlatApplicationDataContainer xPlatContainer;
        private ApplicationDataContainer? container => xPlatContainer.container;

        // Returns a qualified key name "containerName.key" if containerName is not null,
        // which we use to simulate subcontainers on platforms that don't support them.
        private string QualifiedKey(string key)
        {
            return xPlatContainer.containerName != null ? $"{xPlatContainer.containerName}.{key}" : key;
        }

        internal object? this[string key]
        {
#if WINDOWS
            get 
            {
                if (container != null)
                {
                    return container.Values[key];
                }
                else
                {
                    // Unpackaged app - use file storage
                    return xPlatContainer.fileStorage?.GetValue(QualifiedKey(key));
                }
            }
            set 
            {
                if (container != null)
                {
                    container.Values[key] = value;
                }
                else
                {
                    // Unpackaged app - use file storage
                    xPlatContainer.fileStorage?.SetValue(QualifiedKey(key), value);
                }
            }
#else
            get
            {
                // Uno does not support string array values in LocalSettings, so we persist
                // as a string starting with '!' and semi-colon separated
                var value = container!.Values[QualifiedKey(key)];
                if (value is string strValue && strValue != string.Empty && strValue[0] == '!')
                {
                    return strValue[1..].Split(';');
                }
                return value;
            }
            set
            {
                if (value is string[] arrayValue)
                {
                    container!.Values[QualifiedKey(key)] = "!" + string.Join(";", arrayValue);
                }
                else
                {
                    container!.Values[QualifiedKey(key)] = value;
                }
            }
#endif
        }

        internal void Remove(string key)
        {
#if WINDOWS
            if (container != null)
            {
                container.Values.Remove(key);
            }
            else
            {
                // Unpackaged app - use file storage
                xPlatContainer.fileStorage?.RemoveValue(QualifiedKey(key));
            }
#else
            container!.Values.Remove(QualifiedKey(key));
#endif
        }
    }

    // Default constructor for the main container
    internal XPlatApplicationDataContainer() : this(GetApplicationDataContainer(), null)
    {
    }

    // Constructor for sub-containers
    internal XPlatApplicationDataContainer(ApplicationDataContainer? container, string? containerName = null)
    {
        Values = new ValueCollection(this);
        this.container = container;
        this.containerName = containerName;

#if WINDOWS
        // For unpackaged apps where container is null, initialize file storage
        if (container == null && containerName == null)
        {
            fileStorage = new UnpackagedFileStorage();
        }
#endif
    }

    private static ApplicationDataContainer? GetApplicationDataContainer()
    {
#if WINDOWS
        try
        {
            // Try to use ApplicationData.Current for packaged apps
            return ApplicationData.Current.LocalSettings;
        }
        catch (InvalidOperationException)
        {
            // Return null for unpackaged apps
            return null;
        }
#else
        // For non-Windows platforms, use ApplicationData.Current
        return ApplicationData.Current.LocalSettings;
#endif
    }

    private ApplicationDataContainer? container;
    private string? containerName;

#if WINDOWS
    private UnpackagedFileStorage? fileStorage;
#endif

    internal XPlatApplicationDataContainer CreateContainer(string name, ApplicationDataCreateDisposition disposition)
    {
#if WINDOWS
        if (container != null)
        {
            return new XPlatApplicationDataContainer(container.CreateContainer(name, disposition));
        }
        else
        {
            // For unpackaged apps, simulate containers with prefixed keys using the same file storage
            return new XPlatApplicationDataContainer(null, name) { fileStorage = this.fileStorage };
        }
#else
        return new XPlatApplicationDataContainer(container, name);
#endif
    }

    internal void Clear()
    {
#if WINDOWS
        if (container != null)
        {
            container.Values.Clear();
            foreach (var cont in container.Containers)
            {
                DeleteContainer(cont.Key);
            }
        }
        else
        {
            // Unpackaged app - clear file storage
            fileStorage?.Clear();
        }
#else
        container!.Values.Clear();
#endif
    }

#if WINDOWS
    internal void DeleteContainer(string name)
    {
        container?.DeleteContainer(name);
        // For unpackaged apps, containers are simulated with prefixed keys, so no separate action needed
    }
#endif

    internal ValueCollection Values;
}

#if WINDOWS
// File-based storage implementation for unpackaged Windows apps
internal class UnpackagedFileStorage
{
    private readonly string _settingsFilePath;
    private readonly Dictionary<string, object> _values;
    private readonly object _lock = new object();

    public UnpackagedFileStorage()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolderPath = Path.Combine(appDataPath, "HouzLinc");
        Directory.CreateDirectory(appFolderPath);
        
        _settingsFilePath = Path.Combine(appFolderPath, "settings.json");
        _values = new Dictionary<string, object>();
        LoadSettings();
    }

    public object? GetValue(string key)
    {
        lock (_lock)
        {
            return _values.TryGetValue(key, out var value) ? value : null;
        }
    }

    public void SetValue(string key, object value)
    {
        lock (_lock)
        {
            _values[key] = value;
            SaveSettings();
        }
    }

    public void RemoveValue(string key)
    {
        lock (_lock)
        {
            if (_values.Remove(key))
            {
                SaveSettings();
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _values.Clear();
            SaveSettings();
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);
                if (settings != null)
                {
                    foreach (var kvp in settings)
                    {
                        _values[kvp.Key] = ConvertJsonElement(kvp.Value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Failed to load settings: {ex.Message}");
            // If loading fails, start with empty settings
        }
    }

    private void SaveSettings()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_values, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Failed to save settings: {ex.Message}");
        }
    }

    private static object ConvertJsonElement(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => element.GetString()!,
            System.Text.Json.JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
            System.Text.Json.JsonValueKind.Number => element.GetDouble(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            _ => element.ToString()
        };
    }
}
#endif
