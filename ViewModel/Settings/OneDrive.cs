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

using Microsoft.Identity.Client;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Uno.UI.MSAL;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

#if WINDOWS
using Microsoft.Identity.Client.Extensions.Msal;
#endif

namespace ViewModel.Settings;

// TODO: add datamembers to deserialize as the need arises
public sealed class DriveItem
{
    public string? Name { get; set; }
    public DriveFileProps? File { get; set; }
    public bool IsFile => File != null;
}

// This is to allow compile time generation of serialization code
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DriveItem))]
internal partial class DriveItemContext : JsonSerializerContext
{
}

public sealed class DriveFileProps
{
    public string? MimeType { get; set; }
}

// This is to allow compile time generation of serialization code
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DriveFileProps))]
internal partial class DriveFilePropsContext : JsonSerializerContext
{
}

// MSAL configuration value
public record Msal
{
    public string? ClientId { get; init; }
    public string[]? Scopes { get; init; }
    public string? AndroidRedirectUri { get; init; }
}

// MSAL configuration service registered with the host
public class MsalConfiguration
{
    internal readonly Msal msalOptions;

    public MsalConfiguration(IOptions<Msal> options)
    {
        this.msalOptions = options.Value;
    }
}

public sealed class OneDrive
{
    public static string appRootFolderName = string.Empty;

    // MSAL objects
    private IPublicClientApplication? identityClient;
    private AuthenticationResult? session;

    // Application id (or client id) registered with the Microsoft identity platform
    private string clientId = string.Empty;

    // Scopes for which we are getting permission from the user. For a list of scopes:
    // see https://learn.microsoft.com/en-us/onedrive/developer/rest-api/concepts/permissions_reference?view=odsp-graph-online#files-permissions
    private string[] scopes = ["Files.ReadWrite.AppFolder"];

#if WINDOWS
    // MSAL Cache persistence on Windows.
    // On Android and iOS the cache persistence is handled by the platform.
    private const string CacheFileName = "ehouse_msal_cache.txt";
    private readonly static string CacheDir = MsalCacheHelper.UserRootDirectory;
#endif

    // Redicrect URI registered with the application
#if __ANDROID__
    private string redirectUri1 = null!;
#elif __WASM__
    private const string redirectUri1 = "http://localhost:5000/authentication/login-callback.htm";
#elif WINDOWS
    // Sometimes Microsoft requires the localhost URL instead of the nativeclient one.
    // I am not sure when and why and in any case both are registered with the app on Azure.
    // MsalException is thrown in that case in AcquireTokenInteractive.
    // To get around this, we try the nativeclient url first, and loopback if that fails.
    private const string redirectUri1 = "https://login.microsoftonline.com/common/oauth2/nativeclient";
    private const string redirectUri2 = "http://localhost:44321/";
#elif __iOS__
    // TODO: figure out the redirectUri for iOS
    private const string redirectUri1 = "";
#else // DESKTOP
    // TODO: need to understand why on Desktop the redirectUri is supposed to be localhost
    // TODO: Consider switching to Uno's MSALAuthenticationProvider
    // TODO: use #if DESKTOP once this code is moved to the main project
    private const string redirectUri1 = "http://localhost:44321/";
#endif

    // Use Instance to acquire the singleton instance of OneDrive
    private OneDrive() { }

    // Acquire the singleton instance of OneDrive
    public static OneDrive Instance => instance ??= new OneDrive();
    private static OneDrive? instance = null!;

    public void SetMsalConfiguration(MsalConfiguration msalConfiguration)
    {
        clientId = msalConfiguration.msalOptions.ClientId ?? string.Empty;
        if (msalConfiguration.msalOptions.Scopes != null)
        {
            scopes = msalConfiguration.msalOptions.Scopes;
        }
#if __ANDROID__
        if (msalConfiguration.msalOptions.AndroidRedirectUri != null)
        {
            redirectUri1 = msalConfiguration.msalOptions.AndroidRedirectUri;
        }
#endif
    }

    public bool IsTokenExpired()
    {
        if (session == null)
        {
            return true;
        }

        // Check if the token has expired
        return session.ExpiresOn <= DateTimeOffset.UtcNow;
    }
 
    /// <summary>
    /// Authenticate the user and get an access token
    /// </summary>
    /// <returns>Success, token is in _session</returns>
    [MemberNotNullWhen(true, nameof(session))]
    public async Task<bool> ConnectAsync()
    {
        // If we already have authenticated, nothing to do here
        if (session != null && !IsTokenExpired())
            return true;

        var retry = false;
        var redirectUri = redirectUri1;

        do
        {
            if (retry)
            {
                identityClient = null;
                retry = false;
            }

           try
            {
                // If we have not already, create the identity client
                identityClient ??= PublicClientApplicationBuilder.Create(clientId)
                    // For Authorities, see https://learn.microsoft.com/en-us/dotnet/api/microsoft.identity.client.abstractapplicationbuilder-1.withauthority?view=msal-dotnet-latest
                    .WithAuthority(AzureCloudInstance.None, "consumers")
                    .WithRedirectUri(redirectUri)
#if __IOS__
                .WithIosKeychainSecurityGroup("com.microsoft.adalcache")
#endif
                    .WithUnoHelpers()
                    .Build();
            }
            catch (Exception ex)
            {
                Common.Logger.Log.Error($"Error creating Identity Client: {ex.Message}");
                return false;
            }

#if WINDOWS
            // On Android and iOS the MSAL cache persistence is handled by the platform.
            // On Windows we need to handle it ourselves.

            // Build the property to create the coass-platform cache
            var storageProperties =
                new StorageCreationPropertiesBuilder(CacheFileName, CacheDir)
                .Build();

            // This creates and hooks up the cross-platform cache into MSAL
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(identityClient.UserTokenCache);
#if false
            // For debugging, clear the cache
            foreach (var account in await identityClient.GetAccountsAsync())
            {
                await identityClient.RemoveAsync(account);
            }
#endif
#endif

            var accounts = await identityClient.GetAccountsAsync();

            try
            {
                // Adding a timeout as sometimes AcquireTokenSilent hangs
                Task<AuthenticationResult> task = identityClient.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
                Task delay = Task.Delay(TimeSpan.FromSeconds(10));
                Task completedTask = await Task.WhenAny(task, delay);
                if (completedTask == delay)
                {
                    Common.Logger.Log.Error("OneDrive: authentication request timed out!");
                    return false;
                }
                session = task.Result;
            }
            catch (Exception e) when (e is MsalUiRequiredException || e is AggregateException)
            {
                // A MsalUiRequiredException signifies that interactive token acquisition is required
                // On Android, AggregateException can be thrown by AcquireTokenSilent if there are no account in the cache
                try
                {
                    session = await identityClient.AcquireTokenInteractive(scopes)
                        .WithUnoHelpers()
                        .ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    Common.Logger.Log.Debug($"OneDrive: Error Acquiring Token:{System.Environment.NewLine}{msalex}");
#if WINDOWS
                    // On Windows, if we get this exception, try the loopback URL instead (see above notes)
                    if (msalex.ErrorCode == "loopback_redirect_uri")
                    {
                        redirectUri = redirectUri2;
                        retry = true;
                    }
#endif
                }
                catch (Exception ex)
                {
                    Common.Logger.Log.Debug($"OneDrive: Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
                    return false;
                }
            }
        }
        while (retry);

        var success = (identityClient != null && session != null && session.AccessToken != null);
    
        if (success)
        {
            appRootFolderName = await GetSpecialFolderName("approot") ?? string.Empty;
        }

        return success;
    }

    /// <summary>
    /// Save a file to a path in the specified root folder of the default drive
    /// Subfolders are created if they don't exist.
    /// Existing file is overriden.
    /// </summary>
    /// <param name="rootFolder">https://learn.microsoft.com/en-us/onedrive/developer/rest-api/api/drive_get_specialfolder?view=odsp-graph-online</param>
    /// <param name="path">relative path under root folder</param>
    /// <param name="stream">File content to save</param>
    /// <returns>success</returns>
    public async Task<bool> SaveFileAsync(string rootFolder, string path, Stream stream)
    {
        // TODO: the [MemberNotNullWhenTrue] attribute is not working as expected.
        if (!await ConnectAsync() || session == null)
            return false;

        using (var httpClient = new System.Net.Http.HttpClient())
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken);

            try
            {
                stream.Position = 0;
                var content = new StreamContent(stream);
                var response = await httpClient.PutAsync($"https://graph.microsoft.com/v1.0/me/drive/{rootFolder}:/{path}:/content", content);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && await ConnectAsync())
                {
                    response = await httpClient.PutAsync($"https://graph.microsoft.com/v1.0/me/drive/{rootFolder}:/{path}:/content", content);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Save a file to the application root
    /// </summary>
    /// <param name="relativePath">path under the application root</param>
    /// <param name="stream">File content to save</param>
    /// <returns>success</returns>
    public async Task<bool> SaveFileToAppRootAsync(string relativePath, Stream stream)
    {
        return await SaveFileAsync("special/approot", relativePath, stream);
    }

    /// <summary>
    /// Retrieves a DriveItem (e.g., file or folder) from the specified root folder of the default drive
    /// For a file or folder, this contains a list of properties (see definition of DriveItem above)
    /// </summary>
    /// <param name="rootFolder">https://learn.microsoft.com/en-us/onedrive/developer/rest-api/api/drive_get_specialfolder?view=odsp-graph-online</param>
    /// <param name="path">relative path under root folder</param>
    /// <returns>DriveItem found or null</returns>
    public async Task<DriveItem?> GetItemAsync(string rootFolder, string path)
    {
        if (!await ConnectAsync() || session == null)
            return null;

        using (var httpClient = new System.Net.Http.HttpClient())
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken);
            try
            {
                return await httpClient.GetFromJsonAsync($"https://graph.microsoft.com/v1.0/me/drive/{rootFolder}:/{path}", typeof(DriveItem)) as DriveItem;
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.Unauthorized && await ConnectAsync())
                {
                    try
                    {
                        return await httpClient.GetFromJsonAsync($"https://graph.microsoft.com/v1.0/me/drive/{rootFolder}:/{path}", typeof(DriveItem)) as DriveItem;
                    }
                    catch { }
                }
            }
            catch { }
        }

        return null;
    }

    /// <summary>
    /// Return the path under the application root from the relative path of an item
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    public string GetAppRootItemPath(string relativePath)
    {
        return $"Apps/{appRootFolderName}/{relativePath}";
    }

    /// <summary>
    /// Retrieves a DriveItem by relative path under the application root
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns>DriveItem found or null</returns>
    public async Task<DriveItem?> GetItemFromAppRootAsync(string relativePath)
    {
        return await GetItemAsync("special/approot", relativePath);
    }

    /// <summary>
    /// Read a file given by its path under the specified root folder of the default drive
    /// </summary>
    /// <param name="rootFolder">https://learn.microsoft.com/en-us/onedrive/developer/rest-api/api/drive_get_specialfolder?view=odsp-graph-online</param>
    /// <param name="path">relative path under root folder</param>
    /// <returns>Stream with the content of the file, null if failure</returns>
    public async Task<Stream?> ReadFileAsync(string rootFolder, string path)
    {
        if (!await ConnectAsync() || session == null)
            return null;

        using (var httpClient = new System.Net.Http.HttpClient())
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken);

            try
            {
                var response = await httpClient.GetAsync($"https://graph.microsoft.com/v1.0/me/drive/{rootFolder}:/{path}:/content");
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && await ConnectAsync())
                {
                    response = await httpClient.GetAsync($"https://graph.microsoft.com/v1.0/me/drive/{rootFolder}:/{path}:/content");
                }

                return response.IsSuccessStatusCode ? await response.Content.ReadAsStreamAsync() : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Read a file identified by its path relative to the application root
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns>Stream with the content of the file, null if failure</returns>
    public async Task<Stream?> ReadFileFromAppRootAsync(String relativePath)
    {
        return await ReadFileAsync("special/approot", relativePath);
    }

    /// <summary>
    /// Retrieves a special folder (e.g., "special/approot") name
    /// </summary>
    /// <param name="specialFolderName">(https://learn.microsoft.com/en-us/onedrive/developer/rest-api/api/drive_get_specialfolder?view=odsp-graph-online)</param>
    /// <returns>success</returns>
    public async Task<string?> GetSpecialFolderName(string specialFolderName)
    {
        if (!await ConnectAsync() || session == null)
            return null;

        using (var httpClient = new System.Net.Http.HttpClient())
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken);
            try
            {
                var driveItem = await httpClient.GetFromJsonAsync($"https://graph.microsoft.com/v1.0/me/drive/special/{specialFolderName}", typeof(DriveItem)) as DriveItem;
                return driveItem?.Name;
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.Unauthorized && await ConnectAsync())
                {
                    try
                    {
                        var driveItem = await httpClient.GetFromJsonAsync($"https://graph.microsoft.com/v1.0/me/drive/special/{specialFolderName}", typeof(DriveItem)) as DriveItem;
                        return driveItem?.Name;
                    }
                    catch { }
                }
            }
            catch { }
        }
        return null;
    }
}
