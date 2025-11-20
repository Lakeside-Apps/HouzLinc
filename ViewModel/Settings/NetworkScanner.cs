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

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace UnoApp.Utils;

public class NetworkScanner
{
    // Min number of bits set to 1 in the subnet mask.
    // We use this to limit the ip range we scan for performance reason.
    // If the host we are looking for is not in the portion of the range we scan, we will not find it.
    private const int minMaskBits = 22;

    // Max number of scanning threads (kept for heuristic)
    private readonly int maxDegreeOfParallelism = Environment.ProcessorCount * 8;

    // Tunables
    private const int maxInFlightConnects = 64; // cap concurrent connect attempts
    private const int perAddressTimeoutMs = 300; // per-address timeout to avoid long tail

    /// <summary>
    /// Scan all subnets this computer is connected (except loopback and vEthernet) to for a host with the specified port open.
    /// This can only run one scan at a time per instance of NetworkScanner. Returns false if a scan is already ongoing.
    /// </summary>
    /// <param name="port">The port we are looking for</param>
    /// <param name="callback">Passes the IP of the found host, return true to stop the scan if this is the host we were looking for</param>
    /// <returns>True if the host was found, false otherwise</returns>
    public async Task<bool> ScanSubnetsForHostWithOpenPort(int port, Func<IPAddress, Task<bool>> callback)
    {
        if (cts != null)
            return false;

        var subnets = FindNetworks();
        foreach (var subnet in subnets)
        {
            if (await ScanSubnet(subnet.ip, subnet.mask, port, callback).ConfigureAwait(false))
                return true;

            // Uncomment to use UDP broadcast scanning (see notes on it below)
            //if (await NetworkScanner.ScanSubnetWithUdpBroadcast(subnet.ip, subnet.mask, port, callback).ConfigureAwait(false))
            // return true;
        }
        return false;
    }

    /// <summary>
    /// Scan a subnet for a host with the specified port open.
    /// Currently private but could be made public if needed.
    /// </summary>
    /// <param name="subnetIp">Any ip in the subnet</param>
    /// <param name="subnetMask">Subnet mask</param>
    /// <param name="port">The port we are looking for</param>
    /// <param name="callback">See above</param>
    private async Task<bool> ScanSubnet(IPAddress subnetIp, IPAddress subnetMask, int port, Func<IPAddress, Task<bool>> callback)
    {
        Debug.Assert(cts == null);

        bool success = false;
        var scanStartTime = DateTime.Now;

        int maskBits = ComputeSubnetMaskBitCount(subnetMask);
        if (maskBits < 0 || maskBits > 32) return false;
        if (maskBits < minMaskBits) maskBits = minMaskBits;

        Common.Logger.Log.Debug($"Scanning subnet: {subnetIp}, mask: {subnetMask}");

        // Compute the ip range of the subnet, expressed as uints
        var ipBytes = subnetIp.GetAddressBytes();
        Array.Reverse(ipBytes);
        uint ip = BitConverter.ToUInt32(ipBytes, 0);
        uint mask = uint.MaxValue << (32 - maskBits);
        uint ipStart = ip & mask;
        uint ipEnd = ipStart | ~mask;

        // Compute number of ip addresses in the subnet
        Common.Logger.Log.Debug($"Number of IP addresses in the subnet: {ipEnd - ipStart + 1}");

        // create Cancellation token for this scan
        cts = new CancellationTokenSource();
        try
        {
            var options = new ParallelOptions() { CancellationToken = cts.Token, MaxDegreeOfParallelism = maxDegreeOfParallelism };
            await Parallel.ForAsync<uint>(ipStart, ipEnd, options, async (i, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Build IP address bytes directly
                var b0 = (byte)((i >> 24) & 0xFF);
                var b1 = (byte)((i >> 16) & 0xFF);
                var b2 = (byte)((i >> 8) & 0xFF);
                var b3 = (byte)(i & 0xFF);
                var address = new IPAddress(new byte[] { b0, b1, b2, b3 });

                if (await ScanAddressAsync(address, port, perAddressTimeoutMs, cts.Token).ConfigureAwait(false))
                {
                    try
                    {
                        // Invoke callback on the worker thread for performace.
                        // Caller must marshal UI updates, but best to avoid them altogether
                        // as switching can be slow due to ongoing scan of the network.
                        if (await callback(address).ConfigureAwait(false))
                        {
                            try { cts.Cancel(); } catch { /* ignore */ }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.Logger.Log.Debug($"Callback exception: {ex.Message}");
                    }

                    Common.Logger.Log.Debug($"Open port found at {address}:{port}");
                }
            });
        }
        catch (OperationCanceledException)
        {
            Common.Logger.Log.Debug("Host found, subnet scan cancelled.");
            success = true;
        }
        finally
        {
            var scanTime = DateTime.Now - scanStartTime;
            Common.Logger.Log.Debug($"Subnet scan complete or cancelled after {scanTime.Minutes} min, {scanTime.Seconds} sec");
        }

        return success;
    }

    /// <summary>
    /// Scan a subnet for a host with the specified port open using UDP broadcast.
    /// Currently unused: this only works when the host replies to UDP messages and apparently INSTEON hub does not!
    /// </summary>
    /// <param name="subnetIp">Any IP in the subnet</param>
    /// <param name="subnetMask">Subnet mask</param>
    /// <param name="port">The port we are looking for</param>
    /// <param name="callback">Passes the IP of the found host, return true to stop the scan if this is the host we were looking for</param>
    private async Task ScanSubnetWithUdpBroadcast(IPAddress subnetIp, IPAddress subnetMask, int port, Func<IPAddress, Task<bool>> callback)
    {
        Debug.Assert(cts == null);

        var broadcastAddress = GetBroadcastAddress(subnetIp, subnetMask);
        var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        var message = Encoding.ASCII.GetBytes("Discovery message");
        await udpClient.SendAsync(message, message.Length, new IPEndPoint(broadcastAddress, port)).ConfigureAwait(false);

        cts = new CancellationTokenSource();
        var receiveTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync().ConfigureAwait(false);
                var responseIp = result.RemoteEndPoint.Address;
                if (await callback.Invoke(responseIp).ConfigureAwait(false))
                {
                    try { cts?.Cancel(); } catch { /* ignore */ }
                }
            }
        }, cts.Token);

        try
        {
            await receiveTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Common.Logger.Log.Debug("Host found, UDP broadcast scan cancelled.");
        }
        finally
        {
            udpClient.Close();
            cts.Dispose();
            cts = null;
        }
    }

    /// <summary>
    /// Cancel ongoing subnet scan
    /// No effect if no scan is ongoing
    /// </summary>
    public void CancelSubnetScan()
    {
        cts?.Cancel();
    }

    private CancellationTokenSource? cts;

    // Helper to compute a unint bitmask representation of a subnet ip mask
    private static int ComputeSubnetMaskBitCount(IPAddress mask)
    {
        byte[] maskBytes = mask.GetAddressBytes();
        int subnetMask = 0;

        foreach (byte b in maskBytes)
        {
            subnetMask += CountBits(b);
        }

        return subnetMask;
    }

    // Helper to compute the number of bits in the subnet mask
    private static int CountBits(byte b)
    {
        int count = 0;
        while (b != 0)
        {
            count += b & 1;
            b >>= 1;
        }
        return count;
    }

    // Helper to obtain the last address in a subnet ip range, used for UDP broadcasting
    private static IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
    {
        byte[] ipAdressBytes = address.GetAddressBytes();
        byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

        if (ipAdressBytes.Length != subnetMaskBytes.Length)
            throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

        byte[] broadcastAddress = new byte[ipAdressBytes.Length];
        for (int i = 0; i < broadcastAddress.Length; i++)
        {
            broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
        }
        return new IPAddress(broadcastAddress);
    }

    // Helper to find all networks this computer is connected to
    private static List<(IPAddress ip, IPAddress mask)> FindNetworks()
    {
        List<(IPAddress ip, IPAddress mask)> networks = new();
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up)
            {
                // Not interested in loopback interfaces
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    continue;

                // or the hyper-v default switch (NAT)
                if (ni.Name.Contains("vEthernet (Default Switch)", StringComparison.OrdinalIgnoreCase))
                    continue;

                IPInterfaceProperties ipProps = ni.GetIPProperties();
                foreach (UnicastIPAddressInformation ip in ipProps.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        Common.Logger.Log.Debug($"IP Address: {ip.Address}");
                        Common.Logger.Log.Debug($"Subnet Mask: {ip.IPv4Mask}");
                        networks.Add((ip.Address, ip.IPv4Mask));
                    }
                }
            }
        }
        return networks;
    }

    private class ScanTimeOutException : Exception
    {
        public ScanTimeOutException(string msg) : base(msg) { }
    }

    // Helper to scan a specific ip address for a given open port
    private static async Task<bool> ScanAddressAsync(IPAddress ip, int port, int timeoutMs, CancellationToken ct)
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;
            var endPoint = new IPEndPoint(ip, port);
            Common.Logger.Log.Debug($"Scanning: {ip}:{port}");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeoutMs);

            try
            {
                await socket.ConnectAsync(endPoint, timeoutCts.Token).ConfigureAwait(false);
                Common.Logger.Log.Debug($"Open port found at {ip}:{port}");
                return true;
            }
            catch (OperationCanceledException)
            {
                // timeout or cancellation
                return false;
            }
            catch (SocketException)
            {
                return false;
            }
            finally
            {
                try { socket.Close(); } catch { }
            }
        }
        catch (Exception e)
        {
            // keep debug logging minimal
            Common.Logger.Log.Debug(e.Message);
        }
        return false;
    }

    private HttpClient? _verificationClient;

    private HttpClient CreateVerificationClient(string username, string password)
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 1
        };
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(3)
        };

        if (!string.IsNullOrEmpty(username))
        {
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        }

        return client;
    }

    /// <summary>
    /// Quick lightweight verification that the gateway HTTP endpoint responds.
    /// Respects cancellation token and runs without capturing UI context.
    /// </summary>
    public async Task<bool> VerifyHubAsync(string ipAddress, string port, string username, string password, string path = "/", CancellationToken ct = default)
    {
        // Use cached client per gateway to avoid ephemeral port churn
        _verificationClient ??= CreateVerificationClient(username, password);

        // Build URL - use IpAddress/Port fields from Gateway
        var url = new UriBuilder("http", ipAddress, int.Parse(port), path).Uri;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await _verificationClient.SendAsync(req, ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) { return false; }
        catch (HttpRequestException) { return false; }
    }
}
