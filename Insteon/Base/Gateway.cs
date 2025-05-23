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
using Insteon.Model;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace Insteon.Base;

// Provides gateway/hub state and functionality such as
// - IP address, port, hostname
// - user credentials to the gateway
// - can return an HttpClient to the gateway
// - stores and retrieves credentials from the credential store
public sealed class Gateway
{
    internal Gateway(House house)
    {
        this.house = house;
        MacAddress = string.Empty;
        HostName = string.Empty;
        IPAddress = string.Empty;
        Port = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
    }

    internal Gateway(House house, string hostName, string macAddress, string ipAddress, string port)
    {
        this.house = house;
        MacAddress = macAddress;
        HostName = hostName;
        IPAddress = ipAddress;
        Port = port;
    }

    public Gateway(House house, string hostName, string macAddress, string ipAddress, string port, string? username, string? password)
        : this(house, hostName, macAddress, ipAddress, port)
    {
        Username = username;
        Password = password;
    }

    internal Gateway(Gateway gateway)
        : this (gateway.house, gateway.HostName, gateway.MacAddress, gateway.IPAddress, gateway.Port, gateway.Username, gateway.Password)
    {
    }

    ~Gateway()
    {
        httpClient?.Dispose();
    }

    public override bool Equals(object? other)
    {
        if (other is Gateway otherGateway)
        {
            return IsIdenticalTo(otherGateway);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return DeviceId.ToInt();
    }

    internal void CopyFrom(Gateway fromGateway)
    {
        Debug.Assert(DeviceId == fromGateway.DeviceId);
        MacAddress = fromGateway.MacAddress;
        HostName = fromGateway.HostName;
        IPAddress = fromGateway.IPAddress;
        Port = fromGateway.Port;
        Username = fromGateway.Username;
        Password = fromGateway.Password;
    }

    internal bool IsIdenticalTo(Gateway other)
    {
        return DeviceId == other.DeviceId &&
            MacAddress == other.MacAddress &&
            HostName == other.HostName &&
            IPAddress == other.IPAddress &&
            Port == other.Port &&
            Username == other.Username &&
            Password == other.Password;
    }

    internal void OnDeserialized()
    {
        RetrieveCredentials();
    }

    // Called when a command is running
    internal void OnGatewayTraffic(bool hasTraffic)
    {
        house.NotifyGatewayTraffic(hasTraffic);
    }

    // House this gateway belongs to
    // Immutable, e.g., not changed by CopyFrom
    private House house { get; init; }

    // Device ID of this gateway
    // Immutable, e.g., not changed by CopyFrom
    public InsteonID DeviceId { get; init; } = InsteonID.Null;

    /// <summary>
    /// Serialized properties
    /// </summary>
    public string MacAddress { get; set; }
    public string HostName { get; set; }
    public string IPAddress { get; set; }
    public string Port { get; set; }

    /// <summary>
    /// Locally stored properties
    /// </summary>
    public string? Username { get; private set; }
    public string? Password { get; private set; }

    /// <summary>
    /// The httpClient to use to make requests to this gateway/hub
    /// </summary>
    internal HttpClient HttpClient
    {
        get
        {
            if (httpClient == null)
            {
                var handler = new SocketsHttpHandler
                {
                    // the Insteon Hub supports few sockets
                    MaxConnectionsPerServer = 1,

                    // Keep alive but the socket back quickly so that other client (e.g., instances of this app) can use it
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
                };

                httpClient = new HttpClient(handler);
                httpClient.BaseAddress = new Uri("http://" + IPAddress + ":" + Port);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
                string base64 = Base64Encode(Username + ":" + Password);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
                //httpClient.DefaultRequestHeaders.IfModifiedSince = new DateTimeOffset(DateTime.Now.AddDays(-1));
                //httpClient.DefaultRequestHeaders.ConnectionClose = true;
                httpClient.Timeout = TimeSpan.FromSeconds(5);
            }
            return httpClient;
        }
    }
    private HttpClient? httpClient;

    private static string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    /// <summary>
    ///  The response stream for http requests to this gateway/hub
    /// </summary>
    internal InsteonHexStream ResponseStream => responseStream ??= new InsteonHexStream(this);
    private InsteonHexStream? responseStream = null;

    /// <summary>
    /// Credential store management
    /// The view model needs to set this to a concrete implementation
    /// </summary>
    public static IGatewayCredentialLocker? CredentialLocker = null;

    internal void StoreCredentials()
    {
        if (CredentialLocker != null && Username != null && Password != null)
        {
            CredentialLocker.StoreCredentials(DeviceId, Username, Password);
        }
    }

    internal void RetrieveCredentials()
    {
        if (CredentialLocker != null)
        {
            (Username, Password) = CredentialLocker.RetrieveCredentials(DeviceId);
        }
    }

    /// <summary>
    /// For the implementation of the "Clear Hub Credentials" command on the Settings page
    /// </summary>
    public void ClearCredentials()
    {
        if (CredentialLocker != null)
        {
            CredentialLocker.StoreCredentials(DeviceId, string.Empty, string.Empty);
        }
    }
}

public interface IGatewayCredentialLocker
{
    void StoreCredentials(InsteonID insteonID, string username, string password);
    (string? username, string? password) RetrieveCredentials(InsteonID insteonID);
}
