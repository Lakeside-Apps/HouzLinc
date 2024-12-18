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

namespace ViewModel.Settings
{
    // Allows to store and retrieve credentials
    // Use for Insteon Hub

    internal class CredentialLocker
    {
        private const string CredentialContainer = "Credentials";
        static internal void StoreCredentials(string resource, string username, string password)
        {
            if (resource != null)
            {
                // The Windows.Security.Credential API does not work in WinUI. 
                // For now store in clear in the Settings container
                // TODO: Implement encryption/decryption
                SettingsStore.WriteValueToContainer(CredentialContainer, resource + "-username", username);
                SettingsStore.WriteValueToContainer(CredentialContainer, resource + "-password", password);

                /*
                try
                {
                    var vault = new Windows.Security.Credentials.PasswordVault();
                    vault.Add(new Windows.Security.Credentials.PasswordCredential(resource, username, password));
                }
                catch (Exception e)
                {
                    Logger.Log.Debug("Store Credentials failed: " + resource + " " + e.Message);
                }
                */
            }
        }

        static internal bool RemoveCredentials(string resource, string username, string password)
        {
            bool success = false;


            if (resource != null)
            {
                // The Windows.Security.Credential API does not work in WinUI. 
                // For now store in clear in the Settings container
                // TODO: Implement encryption/decryption
                SettingsStore.RemoveValueFromContainer(CredentialContainer, resource + "-username");
                SettingsStore.RemoveValueFromContainer(CredentialContainer, resource + "-password");

                /*
                try
                {
                    var vault = new Windows.Security.Credentials.PasswordVault();
                    vault.Remove(new Windows.Security.Credentials.PasswordCredential(resource, username, password));
                    success = true;
                }
                catch (Exception e)
                {
                    Logger.Log.Debug("Remove Credentials failed: " + resource + " " + e.Message);
                }
                */
            }

            return success;
        }

        static internal (string? username, string? password) TryRetreiveCredentials(string resource)
        {
            string? username = null;
            string? password = null;

            if (resource != null)
            {
                // The Windows.Security.Credential API does not work in WinUI. 
                // For now store in clear in the Settings container
                // TODO: Implement encryption/decryption
                username = SettingsStore.ReadValueFromContainer(CredentialContainer, resource + "-username") as string;
                password = SettingsStore.ReadValueFromContainer(CredentialContainer, resource + "-password") as string;

                /* 
                try
                {
                    var vault = new Windows.Security.Credentials.PasswordVault();
                    IReadOnlyList<Windows.Security.Credentials.PasswordCredential> credentialList = vault.FindAllByResource(resource);
                    if (credentialList != null && credentialList.Count > 0)
                    {
                        credentialList[0].RetrievePassword();
                        username = credentialList[0].UserName;
                        password = credentialList[0].Password;
                    }
                }
                catch (Exception e)
                {
                    Logger.Log.Debug("Retrieve Credentials failed: " + resource + " " + e.Message);
                }
                */
            }

            return (username, password);
        }
    }
}