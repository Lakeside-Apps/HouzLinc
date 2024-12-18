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

namespace ViewModel.Base;

public static class RampRate
{
    public static string[] RampRates =
    {
        "Default",          // 0
        "8 min",            // 1
        "7 min",            // 2
        "6 min",            // 3
        "5 min",            // 4
        "4.5 min",          // 5
        "4 min",            // 6
        "3.5 min",          // 7
        "3 min",            // 8
        "2.5 min",          // 9
        "2 min",            // 10
        "1.5 min",          // 11
        "1 min",            // 12
        "47 sec",           // 13
        "43 sec",           // 14
        "38.5 sec",         // 15
        "34 sec",           // 16
        "32 sec",           // 17
        "30 sec",           // 18
        "28 sec",           // 19
        "26 sec",           // 20
        "23.5 sec",         // 21
        "21.5 sec",         // 22
        "19 sec",           // 23
        "8.5 sec",          // 24
        "6.5 sec",          // 25
        "4.5 sec",          // 26
        "2.0 sec",          // 27
        ".5 sec",           // 28
        ".3 sec",           // 29
        ".2 sec",           // 30
        ".1 sec",           // 31
    };

    public static byte FromString(string str)
    {
        int i = 0;
        foreach(string s in RampRates)
        {
            if (str == s)
            {
                return (byte)i; 
            }
            i++;
        }
        return 0;
    }

    public static string ToString(int value)
    {
        return RampRates[value];
    }
}
