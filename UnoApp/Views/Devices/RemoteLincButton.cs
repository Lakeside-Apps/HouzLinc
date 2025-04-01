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

using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Input;
using ViewModel.Devices;
using Windows.UI.Core;
using System.Diagnostics;

namespace UnoApp.Views.Devices;

/// <summary>
/// This class implements a ToggleButton for the RemoteLinc / Mini-Remote devices.
/// It has two properties to control its label: IsOnButton and Text
/// </summary>
public partial class RemoteLincButton : ToggleButton
{
    public RemoteLincButton() {}
}
