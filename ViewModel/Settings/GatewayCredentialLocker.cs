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

using Common;
using Insteon.Base;

namespace ViewModel.Settings;

// This class stores the credentials for each hub/gateway 
// using the device id of the gateway as the key
public sealed class GatewayCredentialLocker : IGatewayCredentialLocker
{
    internal string GetCredentialResource(InsteonID insteonID)
    {
        return $"InsteonHub({insteonID})";
    }

    public void StoreCredentials(InsteonID insteonID, string username, string password)
    {
        // Delete previously stored crendentials for this gateway, if any and they are different
        (string? savedUsername, string? savedPassword) = CredentialLocker.TryRetreiveCredentials(GetCredentialResource(insteonID));
        if (savedUsername != null && savedPassword != null && (username != savedUsername || password != savedPassword))
        {
            CredentialLocker.RemoveCredentials(GetCredentialResource(insteonID), savedUsername, savedPassword);
        }

        CredentialLocker.StoreCredentials(GetCredentialResource(insteonID), username, password);
    }

    public (string? username, string? password) RetrieveCredentials(InsteonID insteonID)
    {
        return CredentialLocker.TryRetreiveCredentials(GetCredentialResource(insteonID));
    }
}
