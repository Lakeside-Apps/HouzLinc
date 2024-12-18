using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Microsoft.UI.Dispatching;

namespace HouzLinc.Utils;

public class NetworkScanner
{
    // Min number of bits set to 1 in the subnet mask.
    // We use this to limit the ip range we scan for performance reason.
    // If the host we are looking for is not in the portion of the range we scan, we will not find it.
    private const int minMaskBits = 22;

    // Max number of scanning threads
    private static int maxDegreeOfParallelism = Environment.ProcessorCount * 8;

    /// <summary>
    /// Scan all subnets this computer is connected (except loopback and vEthernet) to for a host with the specified port open.
    /// </summary>
    /// <param name="port">The port we are looking for</param>
    /// <param name="callback">Passes the IP of the found host, return true to stop the scan if this is the host we were looking for</param>
    /// <returns>True if the host was found, false otherwise</returns>
    public static async Task<bool> ScanSubnetsForHostWithOpenPort(int port, Func<IPAddress, Task<bool>> callback)
    {
        var subnets = NetworkScanner.FindNetworks();
        foreach (var subnet in subnets)
        {
            if (await NetworkScanner.ScanSubnet(subnet.ip, subnet.mask, 25105, callback))
                return true;

            // Uncomment to use UDP broadcast scanning (see notes on it below)
            //if (await NetworkScanner.ScanSubnetWithUdpBroadcast(subnet.ip, subnet.mask, 25105, callback))
            //    return true;
        }
        return false;
    }

    /// <summary>
    /// Scan a subnet for a host with the specified port open.
    /// </summary>
    /// <param name="subnetIp">Any ip in the subnet</param>
    /// <param name="subnetMask">Subnet mask</param>
    /// <param name="port">The port we are looking for</param>
    /// <param name="callback">See above</param>
    public static async Task<bool> ScanSubnet(IPAddress subnetIp, IPAddress subnetMask, int port, Func<IPAddress, Task<bool>> callback)
    {
        bool success = false;
        var scanStartTime = DateTime.Now;

        int maskBits = ComputeSubnetMaskBitCount(subnetMask);
        if (maskBits < 0 || maskBits > 32) return false;
        if (maskBits < minMaskBits) maskBits = minMaskBits;

        Common.Logger.Log.Debug($"Scaning subnet: {subnetIp}, mask: {subnetMask}");

        // Compute the ip range of the subnet, expressed as uints
        var ipBytes = subnetIp.GetAddressBytes();
        Array.Reverse(ipBytes);
        uint ip = BitConverter.ToUInt32(ipBytes, 0);
        uint mask = uint.MaxValue << (32 - maskBits);
        uint ipStart = ip & mask;
        uint ipEnd = ipStart | ~mask;

        // Compute number of ip addresses in the subnet
        Common.Logger.Log.Debug($"Number of IP addresses in the subnet: {ipEnd - ipStart + 1}");

        DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        var cts = new CancellationTokenSource();
        try
        {
            var options = new ParallelOptions() { CancellationToken = cts.Token, MaxDegreeOfParallelism = maxDegreeOfParallelism};
            await Parallel.ForAsync<uint>(ipStart, ipEnd, options, async (i, cancellationToken) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var bytes = BitConverter.GetBytes(i);
                Array.Reverse(bytes);
                IPAddress address = new IPAddress(bytes);
                if (await ScanAddressAsync(address, port))
                {
                    // invoke callback on the calling thread
                    dispatcherQueue.TryEnqueue(async () =>
                    {
                        if (await callback.Invoke(address))
                        {
                            // We've got the host we were looking for
                            cts.Cancel();
                        }
                    });
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
    /// This only works when the host replies to UDP message and apparently INSTEON hub does not!
    /// </summary>
    /// <param name="subnetIp">Any IP in the subnet</param>
    /// <param name="subnetMask">Subnet mask</param>
    /// <param name="port">The port we are looking for</param>
    /// <param name="callback">Passes the IP of the found host, return true to stop the scan if this is the host we were looking for</param>
    public static async Task ScanSubnetWithUdpBroadcast(IPAddress subnetIp, IPAddress subnetMask, int port, Func<IPAddress, Task<bool>> callback)
    {
        var broadcastAddress = GetBroadcastAddress(subnetIp, subnetMask);
        var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        var message = Encoding.ASCII.GetBytes("Discovery message");
        await udpClient.SendAsync(message, message.Length, new IPEndPoint(broadcastAddress, port));

        var cts = new CancellationTokenSource();
        var receiveTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync();
                var responseIp = result.RemoteEndPoint.Address;
                if (await callback.Invoke(responseIp))
                {
                    cts.Cancel();
                }
            }
        }, cts.Token);

        try
        {
            await receiveTask;
        }
        catch (OperationCanceledException)
        {
            Common.Logger.Log.Debug("Host found, UDP broadcast scan cancelled.");
        }
        finally
        {
            udpClient.Close();
        }
    }

    /// <summary>
    /// Cancel ongoing subnet scan
    /// </summary>
    public static void CancelSubnetScan()
    {
        cts.Cancel();
    }

    private static CancellationTokenSource cts = new();

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
    private static async Task<bool> ScanAddressAsync(IPAddress ip, int port)
    {
        try
        {
            using (TcpClient client = new TcpClient())
            {
                client.NoDelay = true;
                IPEndPoint endPoint = new IPEndPoint(ip, port);
                var cancelTask = Task.Delay(1000);
                Common.Logger.Log.Debug($"Scanning: {ip}:{port}");
                var connectTask = client.ConnectAsync(ip, port);

                // double await so if cancelTask throws exception, this throws it
                await Task.WhenAny(connectTask, cancelTask);

                if (connectTask.IsCompleted && !connectTask.IsFaulted)
                {
                    Common.Logger.Log.Debug($"Open port found at {ip}:{port}");
                    return true;
                }
                else
                {
                    throw new ScanTimeOutException($"Scan timed out at {ip}:{port}");
                }
            }
        }
        catch (Exception e)
        {
            Common.Logger.Log.Debug(e.Message);
        }
        return false;
    }
}
