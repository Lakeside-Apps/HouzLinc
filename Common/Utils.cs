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

namespace Common
{
    /// <summary>
    /// A set of simple utilities
    /// </summary>
    public class Utils
    {
        // Returns the byte represented by a two digit hexadecimal string
        public static byte ByteFromString(string s)
        {
            AssertArgument(s.Length <= 2, "ByteFromString: string needs to be two characters or less");
            return ByteFromString(s, 0);
        }

        // Extract a byte from a two (optionally one) digit hex number at position i of string s
        public static byte ByteFromString(string s, int i, bool requires2Digits = false)
        {
            AssertInRange(i, 0, s.Length, "ByteFromString: out of bounds");

            if (s.Length > i + 1)
            {
                return (byte)((GetHexVal(s[i]) << 4) + (GetHexVal(s[i + 1])));
            }
            else if (!requires2Digits && s.Length > i)
            {
                return (byte)(GetHexVal(s[i]));
            }
            else
            {
                throw new InvalidInsteonIDException();
            }
        }

        // Get an array of bytes from a string of hex digits
        public static byte[] BytesFromString(string s)
        {
            return BytesFromString(s, 0, s.Length);
        }

        public static byte[] BytesFromString(string s, int start, int count)
        {
            if (start >= s.Length || start + count > s.Length)
            {
                throw new ArgumentOutOfRangeException("BytesFromString: out of bounds");
            }

            byte[] bytes = new byte[count / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)((GetHexVal(s[start + (i * 2)]) << 4) + (GetHexVal(s[start + (i * 2) + 1])));
            }

            return bytes;
        }

        static int GetHexVal(char hex)
        {
            if (hex >= '0' && hex <= '9')
                return hex - '0';
            if (hex >= 'A' && hex <= 'F')
                return hex - 'A' + 10;
            if (hex >= 'a' && hex <= 'f')
                return hex - 'a' + 10;
            throw new ArgumentException("ByteFromString: character must be hex digit");
        }

        public static string ByteToString(byte b)
        {
            return b.ToString("%h");
        }

        public static void Assert(bool expression, string message)
        {
            if (!expression)
            {
                throw new Exception(message);
            }
        }

        public static void Assert<TException>(bool expression) where TException : Exception, new()
        {
            if (!expression)
            {
                throw new TException();
            }
        }

        public static void Assert<TException>(bool expression, string message) where TException : Exception
        {
            if (!expression)
            {
                throw (Activator.CreateInstance(typeof(TException), message) as TException)!;
            }
        }

        public static void AssertArgument(bool expression, string message)
        {
            if (!expression)
            {
                throw new ArgumentException(message);
            }
        }

        public static void AssertInRange(int n, int start, int length, string message)
        {
            if (n < start || n >= start + length)
            {
                throw new ArgumentOutOfRangeException(message);
            }
        }

    }
}
