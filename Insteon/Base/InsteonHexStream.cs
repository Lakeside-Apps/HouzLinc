using System.Diagnostics;

namespace Insteon.Base;

/// <Summary>
/// This class builds on CircularBufferHexStream by implementing the means to get data via an http request to the Insteon Hub
/// </Summary>
internal class InsteonHexStream : CircularBufferHexStream
{
    /// <summary>
    ///  Caller manages the lifteime of httpClient
    /// </summary>
    /// <param name="gateway"></param>
    internal InsteonHexStream (Gateway gateway) : base(contentBufferLength)
    {
        this.gateway = gateway;
    }

    /// <summary>
    /// Clears the content buffer
    /// </summary>
    /// <exception cref="HttpRequestException">thrown on http request error</exception>
    /// <exception cref="System.Net.WebException">thrown on http request error on Android</exception>
    /// <exception cref="Exception">throw on timeout</exception>
    /// <returns></returns>
    protected override async Task ClearBuffer()
    {
        HttpResponseMessage httpResponse = await gateway.HttpClient.GetAsync("1?XB=M=1");

        if (httpResponse.IsSuccessStatusCode)
        {
            // Reset the response stream, read the response and check that it is all zeros
            Reset();
        }
    }

    /// <summary>
    /// Request data from the hub, request the content of the circular buffer
    /// </summary>
    /// <exception cref="HttpRequestException">thrown on http request error</exception>
    /// <exception cref="System.Net.WebException">thrown on http request error on Android</exception>
    /// <exception cref="Exception">throw on timeout</exception>
    /// <returns>the data</returns>
    protected override async Task<string> GetData()
    {
        if (DateTime.Now - lastGetDataTime < minTimeBetweenGetData)
        {
            var waitTime = minTimeBetweenGetData - (DateTime.Now - lastGetDataTime);
            //Logger.Log.Debug("Waiting " + waitTime.TotalMilliseconds + "ms to get data");
            await Task.Delay(waitTime);
        }

        string responseXML;

        //Logger.Log.Debug("Getting data");
        responseXML = await gateway.HttpClient.GetStringAsync("buffstatus.xml");

        // returned XML format is <response><BS>response text</BS></response>
        string contentBuffer = responseXML.Split(new String[2] { "<BS>", "</BS>" }, 3, StringSplitOptions.None)[1];

        if (contentBuffer.Length > contentBufferLength)
        {
            contentBuffer = contentBuffer.Substring(0, contentBufferLength);
        }

        lastGetDataTime = DateTime.Now;

        return contentBuffer;
    }

    // Length of the circular buffer in characters.
    // If response longer, enforce 200 characters (100 bytes).
    // Older Hubs return 100 characters, each an hexadecimal digit (50 bytes).
    // Newer ones return 200 characters (100 bytes) + 2 more at the end (1 byte) that need to be discarded.
    // We only support new hubs.
    private const int contentBufferLength = 200;

    // Client to use to make web requests
    // Lifetime is control by creator of this InsteonHexStream
    private Gateway gateway;

    // Time of last call to GetData
    private DateTime lastGetDataTime = DateTime.Now;

    // Minimum intervall of time between GetData calls
    // Below 20ms we start seeing incomplete data being returned
    static private TimeSpan minTimeBetweenGetData = TimeSpan.FromMilliseconds(20);
}
