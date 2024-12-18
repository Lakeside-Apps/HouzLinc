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

namespace Insteon.Base;

/// <summary>
///  This class defines a string of hexadecimal digit characters representing a sequence of bytes with 2 characters per byte.
///  It is used to express responses from the Insteon Hub.
///  It is immutable in that the underlying string cannot be changed after creation.
///  Several methods allow to read one or more bytes starting at a given index, in either string or byte form.
///  Byte index are 1-based to match Insteon specification.
/// </summary>
internal class HexString
{
    string _charString;

    internal HexString(string s)
    {
        _charString = s;
    }

    internal HexString(HexString hexString)
    {
        _charString = hexString._charString;
    }

    public override string ToString()
    {
        return _charString;
    }

    internal int ByteCount { get { return _charString.Length / 2; } }

    internal string ByteAsString(int byteIndex)
    {
        return _charString.Substring(CharPos(byteIndex), 2);
    }

    internal string BytesAsString(int byteIndex, int byteCount)
    {
        int charPos = CharPos(byteIndex);
        int charCount = byteCount * 2;
        Utils.AssertInRange(charPos + charCount - 1, 0, _charString.Length, "HexString: out of bounds access");
        return _charString.Substring(charPos, charCount);
    }

    internal byte Byte(int byteIndex)
    {
        return Utils.ByteFromString(_charString, CharPos(byteIndex));
    }

    internal byte[] Bytes()
    {
        return Utils.BytesFromString(_charString, 0, _charString.Length);
    }

    internal byte[] Bytes(int byteIndex, int byteCount)
    {
        return Utils.BytesFromString(_charString, CharPos(byteIndex), byteCount * 2);
    }

    internal HexString SubHexString(int byteIndex, int byteCount)
    {
        return new HexString(_charString.Substring(CharPos(byteIndex), byteCount * 2));
    }

    /// Private members
    int CharPos(int byteIndex)
    {
        // Each byte is represented by two characters 
        int charPos = (byteIndex - 1) * 2;
        Utils.AssertInRange(charPos, 0, _charString.Length, "HexMessage: out of bounds access");
        return charPos;
    }
}
