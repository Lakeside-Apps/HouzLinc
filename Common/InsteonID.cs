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

namespace Common;

/// <summary>
/// Exception thrown when encountering an invalid Insteon ID
/// </summary>
public sealed class InvalidInsteonIDException : Exception
{
    public InvalidInsteonIDException() : base("Invalid Insteon ID")
    {
    }
}

/// <summary>
/// Comparer to sort on InsteonID values
/// </summary>
public sealed class InsteonIDComparer : IComparer<InsteonID>
{
    public int Compare(InsteonID? x, InsteonID? y)
    {
        return InsteonID.Compare(x, y);
    }
}

/// <summary>
/// A class to represent an Insteon ID
/// This is an immutable object so it is safe to copy by reference
/// Can be parsed from and output to a string, either in human readble form "xx.yy.zz" or in hex command string form (XXYYZZ)
/// Can be converted to a array of bytes or to an int
/// This class also allow to compare Insteon ID's for equality
/// </summary>
public sealed class InsteonID : IEquatable<InsteonID>
{
    public InsteonID()
    {
    }

    public static InsteonID Null => new InsteonID();

    public InsteonID(byte high, byte mid, byte low)
    {
        High = high;
        Mid = mid;
        Low = low;
    }

    public InsteonID(uint value)
    {
        uintValue = value & 0xFFFFFF;
    }

    public InsteonID(InsteonID id)
    {
        this.uintValue = id.uintValue;
    }

    public InsteonID(string s)
    {
        if (s.Contains("."))
        {
            FromUserString(s);
        }
        else
        {
            FromCommandString(s);
        }
    }

    public bool IsNull => uintValue == 0;
    public bool IsValid => !IsNull;

    public byte High
    {
        get => (byte)((uintValue & 0xFF0000) >> 16);
        set
        {
            uintValue &= 0x00FFFF; 
            uintValue |= (uint)(value << 16);
        }
    }

    public byte Mid
    {
        get => (byte)((uintValue & 0x00FF00) >> 8);
        set
        {
            uintValue &= 0xFF00FF;
            uintValue |= (uint)(value << 8);
        }
    }

    public byte Low
    {
        get => (byte)((uintValue & 0x0000FF));
        set
        {
            uintValue &= 0xFFFF00;
            uintValue |= (uint)(value);
        }
    }

    public override string ToString()
    {
        return High.ToString("X2") + "." + Mid.ToString("X2") + "." + Low.ToString("X2");
    }

    public string ToCommandString()
    {
        return High.ToString("X2") + Mid.ToString("X2") + Low.ToString("X2");
    }

    public static InsteonID FromString(string s)
    {
        return new InsteonID(s);
    }

    private void FromCommandString(string s)
    {
        try
        {
            High = Utils.ByteFromString(s, 0, requires2Digits: true);
            Mid = Utils.ByteFromString(s, 2, requires2Digits: true);
            Low = Utils.ByteFromString(s, 4, requires2Digits: true);
        }
        catch (Exception)
        {
            throw new InvalidInsteonIDException();
        }
    }

    private void FromUserString(string s)
    {
        try
        {
            string[] Values = s.Split(new char[] { '.' }, 3);
            if (Values.Length != 3)
            {
                throw new InvalidInsteonIDException();
            }

            High = Utils.ByteFromString(Values[0]);
            Mid = Utils.ByteFromString(Values[1]);
            Low = Utils.ByteFromString(Values[2]);
        }
        catch (Exception)
        {
            throw new InvalidInsteonIDException();
        }
    }

    public void ToByteArray(Byte[] Array, int i)
    {
        Array[i] = High;
        Array[i+1] = Mid;
        Array[i+2] = Low;
    }

    public int ToInt()
    {
        return (int)uintValue;
    }

    public static bool operator ==(InsteonID? id1, object? id2)
    {
        return ReferenceEquals(id1, null) ? ReferenceEquals(id2, null) : id1.Equals(id2);
    }

    public static bool operator !=(InsteonID? id1, object? id2)
    {
        return !(id1 == id2);
    }

    public override bool Equals(object? obj)
    {
        InsteonID? id = obj as InsteonID;
        if (!ReferenceEquals(id, null))
        {
            return this.Equals(id);
        }

        string? idText = obj as string;
        if (idText != null)
        {
            return this.Equals(idText);
        }

        return false;
    }

    public bool Equals(InsteonID? id)
    {
        return ReferenceEquals(id, null) ? this.IsNull : (this.uintValue == id.uintValue);
    }

    public bool Equals(string idText)
    {
        return Equals(new InsteonID(idText));
    }

    public static bool operator >(InsteonID? id1, object? id2)
    {
        return Compare(id1, id2 as InsteonID) > 0;
    }

    public static bool operator <(InsteonID? id1, object? id2)
    {
        return Compare(id1, id2 as InsteonID) < 0;
    }

    public static bool operator >=(InsteonID? id1, object? id2)
    {
        return Compare(id1, id2 as InsteonID) >= 0;
    }

    public static bool operator <=(InsteonID? id1, object? id2)
    {
        return Compare(id1, id2 as InsteonID) <= 0;
    }

    public static int Compare(InsteonID? x, InsteonID? y)
    {
        if (ReferenceEquals(x, null))
            return (ReferenceEquals(y, null) || y.IsNull) ? 0 : -1;
        else if (ReferenceEquals(y, null))
            return (ReferenceEquals(x, null) || x.IsNull) ? 0 : 1;

        if (x.High > y.High) { return 1; }
        else if (x.High < y.High) { return -1; }
        else if (x.Mid > y.Mid) { return 1; }
        else if (x.Mid < y.Mid) { return -1; }
        else if (x.Low > y.Low) { return 1; }
        else if (x.Low < y.Low) { return -1; }
        else return 0;
    }

    public override int GetHashCode()
    {
        return ToInt();
    }

    // For testing purposes
    // Check if this is a XX.YY.ZZ ID
    public bool IsXXYYZZ()
    {
        var s = this.ToCommandString();
        return s[0] == s[1] && s[2] == s[3] && s[4] == s[5];
    }

    private uint uintValue;
}
