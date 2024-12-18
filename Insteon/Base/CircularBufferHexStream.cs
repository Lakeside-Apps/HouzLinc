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
using System.Diagnostics.CodeAnalysis;
using Common;

namespace Insteon.Base;

/// <Summary>
/// This class implements a circular buffer of bytes, each expressed as 2 hexadecimal characters.
/// It is designed to overlay the proper access logic over responses from the Insteon Hub, referred
/// to here as the data provider. For testing we use a fake data provider.
/// The underlying content is stored as a string containing an even number of characters, each 
/// representing an hex digit.
/// Derived classes must implement two methods to get data from the provider:
/// - GetData(), which retrieve more bytes from the underlyng response stream,
/// - ClearBuffer(), which instructs the response stream provider to clear the buffer.
/// </Summary>
internal abstract class CircularBufferHexStream : IHexStream
{
    private protected CircularBufferHexStream(int bufferLength)
    {
        _contentBufferLength = bufferLength;
    }

    /// <summary>
    /// Get data from the provider to fill the content buffer 
    /// Derived classes override
    /// </summary>
    protected abstract Task<string> GetData();

    /// <summary>
    /// Clear the content buffer
    /// Derived classes override
    /// </summary>
    protected abstract Task ClearBuffer();

    /// <summary>
    /// Reset the stream at the begining of the buffer
    /// </summary>
    public void Reset()
    {
        _currentCharPos = 0;
        _newCharPos = 0;
    }

    /// <summary>
    /// Clear the buffer and reset the stream
    /// </summary>
    public async Task Clear()
    {
        await ClearBuffer();
        Reset();
    }

    /// <summary>
    ///  Read n bytes from the stream
    /// </summary>
    /// <param name="byteCount">Number of bytes to read, 0 to read a full content buffer</param>
    /// <param name="acquireData">Acquire data from the underlying data provider</param>
    /// <returns>Hexstring containing the bytes read</returns>
    public async Task<HexString> Read(int byteCount, bool acquireData = false)
    {
        if (acquireData || _contentBuffer.Length == 0)
        {
            _previousBuffer = _contentBuffer;
            _contentBuffer = await GetData();
        }

        // Two characters, each an hexadecimal digit, make a byte
        int charCount;

        // If zero byte count was passed in, we are reading the entire buffer
        if (byteCount == 0)
        {
            charCount = _contentBuffer.Length;
            byteCount = charCount / 2;
        }
        else
        {
            charCount = byteCount * 2;
        }

        Debug.Assert(charCount <= _contentBuffer.Length);

        // Compute the current position after the read
        _newCharPos = _currentCharPos + charCount;

        // If we have fresh new content, write the current status of the buffer to debug console
        if (Logger.Log.IsEnabled(LogEventId.Debug) &&
            (!acquireData || !_contentBuffer.Equals(_previousBuffer, StringComparison.Ordinal)))
        {
            Logger.Log.Debug("Reading " + byteCount + (byteCount > 1 ? " bytes" : " byte") + " from 'C' to 'N'");
            WriteToDebug();
        }

        // Now return the requested number of bytes as an HexString
        HexString ret;
        int realCharPos = _currentCharPos % _contentBuffer.Length;
        if (realCharPos + charCount < _contentBuffer.Length)
        {
            ret = new HexString(_contentBuffer.Substring(realCharPos, charCount));
        }
        else
        {
            ret = new HexString(_contentBuffer.Substring(realCharPos, _contentBuffer.Length - realCharPos) + _contentBuffer.Substring(0, charCount - (_contentBuffer.Length - realCharPos)));
        }

        return ret;
    }

    /// <summary>
    ///  Advance to the new position after the last read
    /// </summary>
    public void Advance()
    {
        // Update the new current position
        _currentCharPos = _newCharPos;
        Logger.Log.Debug("Advancing 'C' to 'N'");
    }

    /// <summary>
    ///  Advance by a given number of bytes
    /// </summary>
    /// <param name="byteCount">Number of bytes to advance by</param>
    public void Advance(int byteCount)
    {
        // Update the new current position
        _newCharPos = _currentCharPos + byteCount * 2;
        _currentCharPos = _newCharPos;
        Logger.Log.Debug("Advancing 'C' and 'N' by " + byteCount + " bytes");
    }

    // Write the content of the buffer and the new and current positions to the debug logger
    private void WriteToDebug()
    {
        int c = _currentCharPos % _contentBuffer.Length;
        int n = _newCharPos % _contentBuffer.Length;

        // Write the contents of the buffer.
        // Attempt to trim '0' characters at the end of the buffer to make it more readable
        // and avoid wrapping to the next line.
        string b = _contentBuffer.TrimEnd('0');
        var l = b.Length;
        if (l < _contentBufferLength/2) l = _contentBufferLength/2;
        if (l < n) l = n;
        b = b.PadRight(l, '0');
        if (l < _contentBufferLength) b = b + "...";
        Logger.Log.Debug(b);

        // Write the current and new positions just under the buffer content
        if (c < n)
        {
            string s = "N";
            s = s.PadLeft(n - c);
            s = "C" + s;
            Logger.Log.Debug(s.PadLeft(n + 1));
        }
        else if (c > n)
        {
            string s = "C";
            s = s.PadLeft(c - n);
            s = "N" + s;
            Logger.Log.Debug(s.PadLeft(c + 1));
        }
        else
        {
            string s = "C";
            Logger.Log.Debug(s.PadLeft(c + 1));
        }
    }

    public override string ToString()
    {
        return _contentBuffer?.ToString() ?? string.Empty;
    }

    // the underlying "circular" content buffer
    // contains at most the last _contentBuffer.Length characters of the content
    string _contentBuffer = string.Empty;

    // Size of the circular buffer in characters
    int _contentBufferLength;

    // saved content buffer before getting data
    string? _previousBuffer;

    // the current position in the content 
    int _currentCharPos;

    // the new position in the content after a read
    int _newCharPos;
}
