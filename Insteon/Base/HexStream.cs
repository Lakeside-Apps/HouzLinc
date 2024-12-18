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

namespace Insteon.Base;

/// <summary>
/// Interface to a stream of bytes
/// </summary>
internal interface IHexStream
{
    /// <summary>
    /// Reset position at the begining of the stream
    /// </summary>
    void Reset();
    
    /// <summary>
    /// Read byteCount bytes from the stream,
    /// optionally asking the underlying data provider for more data
    /// This method does not advance the current reading position so calling it 
    /// multiple times will return the same values. Use Advance() to advance the 
    /// current position.
    /// </summary>
    /// <param name="byteCount">number of bytes to read</param>
    /// <param name="acquireData">true to ask data provider for more data</param>
    /// <returns>Promise to read data</returns>
    Task<HexString> Read(int byteCount, bool acquireData);

    /// <summary>
    /// Advance reading position to the end of previously read data
    /// </summary>
    void Advance();

    /// <summary>
    /// Advance reading position by byteCount bytes
    /// </summary>
    /// <param name="byteCount"></param>
    void Advance(int byteCount);
}
