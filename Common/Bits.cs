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

using Windows.ApplicationModel.Chat;

namespace Common
{
    /// <summary>
    /// A class to conveniently set and retrieve bits in an int
    /// </summary>
    public struct Bits
    {
        public Bits(int value)
        {
            this.value = value;
        }

        public static implicit operator int(Bits bits)
        {
            return bits.value;
        }

        public static implicit operator byte(Bits bits)
        {
            return (byte)bits.value;
        }

        public static implicit operator Bits(int value)
        {
            return new Bits(value);
        }

        public bool this[int index]
        {
            get { return (this.value & (1 << index)) != 0; }
            set { if (value) { this.value |= (1 << index); } else { this.value &= ~(1 << index); } }
        }

        public string ToString(string f)
        {
            return value.ToString(f);
        }

        public override string ToString()
        {
            return "0x" + ToString("X2");
        }

        public override bool Equals(object? other)
        {
            if (other is Bits otherBits)
            {
                return value == otherBits.value;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static bool operator == (Bits a, Bits b)
        {
            return a.Equals(b);
        }

        public static bool operator != (Bits a, Bits b)
        {
            return !a.Equals(b);
        }

        private int value;
    }
}
