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

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Common;
public class ObservableObject : INotifyPropertyChanged
{
    // Data binding support
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        // Raise the PropertyChanged event, passing the name of the property whose value has changed.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
