using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Insteon.Model;

namespace ViewModel.Settings;

/// <summary>
/// This class represent a storage provider for saving and loading a house configuration.
/// Derived classes implement the functionality against a specific storage provider, 
/// such as File or OneDrive for example.
/// </summary>
public abstract class StorageProvider
{
    /// <summary>
    /// Name of the provide class for persistence in Settings
    /// </summary>
    internal abstract string ProviderName { get; }

    /// <summary>
    /// Show necessary UI to pick a new storage location, e.g., new file
    /// Some provider always use the same location and don't need this functionality
    /// </summary>
    /// <returns></returns>
    internal abstract Task<object?> PickNewStorage();

    /// <summary>
    /// Show necessary UI to pick an existing storage location (e.g., existing file)
    /// Some provider always use the same location and don't need this functionality
    /// </summary>
    /// <returns></returns>
    internal abstract Task<object?> PickExistingStorage();

    /// <summary>
    /// Returns the path of the current storage
    /// </summary>
    /// <param name="storage">The storage (e.g., FileStorage) or nothing for providers 
    /// always predetermined storage location.
    /// </param>
    /// <returns></returns>
    internal abstract string GetStoragePath(object? storage);

    /// <summary>
    /// Load the house configuration from the storage
    /// </summary>
    /// <param name="storage">Provider speicif, represent the storage.
    /// Null if the provider always uses the same storage location</param>
    /// <param name="isNew">Creating a new house location in a new storage</param>
    /// <returns>the house configuration (model), null if fails</returns>
    internal abstract Task<House?> LoadFromStorage(object? storage, bool isNew);

    /// <summary>
    /// Save the house configuration to the storage
    /// </summary>
    /// <param name="storage">Provider speicif, represent the storage.
    /// Null if the provider always uses the same storage location</param>
    /// <param name="house">the house configuration (model)</param>
    /// <returns></returns>
    internal abstract Task<bool> SaveToStorage(object? storage, House house);
}
