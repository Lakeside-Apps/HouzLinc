using System.Diagnostics;
using Common;
using Insteon.Serialization.Houselinc;
using Insteon.Model;


namespace ViewModel.Settings;
internal class OneDriveStorageProvider : StorageProvider
{
    internal const string Name = "OneDrive";
    internal override string ProviderName => Name;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    internal override async Task<object?> PickExistingStorage()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        return null;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    internal override async Task<object?> PickNewStorage()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        return null;
    }

    internal override string GetStoragePath(object? storage)
    {
        return $"OneDrive/{HouseFilePathOnOneDrive}";
    }

    internal override async Task<House?> LoadFromStorage(object? storage, bool isNew)
    {
        if (!await TryConnectToOneDriveAsync())
        {
            return null;
        }
        else if (!await HouseFileExistsOnOneDrive())
        {
            // Create new house configuration
            var house = new House().EnsureNew();
            return (await SaveToStorage(storage, house)) ? house : null;
        }
        else
        {
            return await LoadHouseFromOneDrive();
        }
    }

    internal override async Task<bool> SaveToStorage(object? storage, House house)
    {
        return await MergeAndSaveHouseToOneDrive(house);
    }

    // Name of the file used to save the house configuration (model)
    private const string HouseFileName = "houselinc.xml";

    // House configuration (model) file path on OneDrive
    private static string HouseFilePathOnOneDrive => OneDrive.Instance.GetAppRootItemPath(HouseFileName);

    // Attempt to connect to OneDrive
    private static async Task<bool> TryConnectToOneDriveAsync()
    {
        return await OneDrive.Instance.ConnectAsync();
    }

    // Check if given file exist on OneDrive
    private static async Task<bool> HouseFileExistsOnOneDrive()
    {
        return await OneDrive.Instance.GetItemFromAppRootAsync(HouseFileName) != null;
    }

    /// Loads the House configuration (model) from a preset file in the App root of OneDrive
    /// Only houselinc.xml like format supported at this time
    private static async Task<House?> LoadHouseFromOneDrive()
    {
        using (var stream = await OneDrive.Instance.ReadFileFromAppRootAsync(HouseFileName))
        {
            if (stream != null)
            {
                var house = await HLSerializer.Deserialize(stream);
                if (house != null)
                {
                    // If serialization indicated that we have to round trip immediatly back to storage
                    // (e.g., to persist back a model format change), handle it
                    if (house.RequestSaveAfterLoad && !await SaveHouseToOneDrive(house))
                        return null;

                    Logger.Log.Debug("Model Loaded");
                    return house;
                }
            }
        }

        return null;
    }

    // Saves the house configuration (model) to a file in the App root of OneDrive
    public static async Task<bool> SaveHouseToOneDrive(House house)
    {
        using (var stream = new MemoryStream())
        {
            if (await HLSerializer.Serialize(stream, house))
            {
                if (await OneDrive.Instance.SaveFileToAppRootAsync(HouseFileName, stream))
                {
                    Logger.Log.Debug("Model saved");
                    return true;
                }
            }
        }
        return false;
    }

    // <summary>
    // To allow multiple instances of the app to work concurrently on the same model file.
    // Merge in outside changes and then saves the model to a file in the App root of OneDrive.
    // Only houselinc.xml like format supported at this time
    // </summary>
    // <returns>success</returns>
    public static async Task<bool> MergeAndSaveHouseToOneDrive(House house)
    {
        House? lastSavedHouse = null;
        using (var stream = await OneDrive.Instance.ReadFileFromAppRootAsync(HouseFileName))
        {
            if (stream != null)
            {
                // First load the house model last saved by any other instance of this app
                lastSavedHouse = await HLSerializer.Deserialize(stream);
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
            }
        }

        return await SaveHouseToOneDrive(house);
    }
}
