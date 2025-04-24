using Common;
using Insteon.Serialization.Houselinc;
using Insteon.Model;
using System.Diagnostics;

namespace ViewModel.Settings;

public class FileStorageProvider : StorageProvider
{
    /// <summary>
    /// Event to App layer to show the file open picker
    /// </summary>
    /// <returns></returns>
    public static event Func<Task<StorageFile>> ShowFileOpenPickerHandler = null!;

    /// <summary>
    /// Event to App layer to show the file save picker
    /// </summary>
    /// <returns></returns>
    public static event Func<Task<StorageFile>> ShowFileSavePickerHandler = null!;

    internal const string Name = "File";
    internal override string ProviderName => Name;

    internal override async Task<object?> PickNewStorage()
    {
        return await ShowFileSavePickerHandler();
    }

    internal override async Task<object?> PickExistingStorage()
    {
        return await ShowFileOpenPickerHandler();
    }

    internal override string GetStoragePath(object? storage)
    {
        return (storage as StorageFile)?.Path ?? "";
    }

    internal override async Task<House?> LoadFromStorage(object? storage, bool isNew)
    {
        if (storage is StorageFile file)
        {
            if (isNew)
            {
                var house = new House().EnsureNew();
                return await SaveHouseToFile(file, house) ? house : null;
            }
            else
            {
                return await LoadHouseFromFile(file);
            }
        }
        return null;
    }

    internal override async Task<bool> SaveToStorage(object? storage, House house)
    {
        if (storage is StorageFile file)
        {
            return await MergeAndSaveHouseToFile(file, house);
        }
        return false;
    }

    // Loads the House configuration (model) from a local file
    // Only houselinc.xml like format supported at this time
    private static async Task<House?> LoadHouseFromFile(StorageFile file)
    {
        var house = await HLSerializer.Deserialize(file);
        if (house != null)
        {
            // If serialization indicated that we have to round trip immediatly back to storage
            // (e.g., to persist back a model format change), handle it
            if (house.RequestSaveAfterLoad && !await SaveHouseToFile(file, house))
                return null;

            Logger.Log.Debug("Model Loaded");
            return house;
        }

        return null;
    }

    // Save the house configuration (model) to a local file
    private static async Task<bool> SaveHouseToFile(StorageFile file, House house)
    {
        // Then save it back
        if (await HLSerializer.Serialize(file, house))
        {
            Logger.Log.Debug("Model saved");
            return true;
        }
        return false;
    }

    // To allow multiple instances of the app to work concurrently on the same model file,
    // merge in changes made by another instance of the app and then saves the model back to a file.
    // Only houselinc.xml like format supported at this time
    private static async Task<bool> MergeAndSaveHouseToFile(StorageFile file, House house)
    {
        // First load the house model last saved by any other instance of this app
        var lastSavedHouse = await HLSerializer.Deserialize(file);
        if (lastSavedHouse != null)
        {
            // Merge in our local changes since we last saved the model
            house.PlayChanges(lastSavedHouse);

            // The result is a new model that is a merge of the last saved model and our local changes
            // We copy it back to our working house model, allowing UI notificaitons for what may have changed
            house.CopyFrom(lastSavedHouse);

#if DEBUG
            // TEMPORARY: Check that the merge worked
            // (assumes the file was not modified by another instance of the app)
            Debug.Assert(house.IsIdenticalTo(lastSavedHouse));
#endif
        }

        return await SaveHouseToFile(file, house);
    }
}
