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

using Insteon.Model;
using Insteon.Base;
using System.Diagnostics;

namespace ViewModel.Devices.ControlLinc;

/// <summary>
/// ControlLinc, ICON Tabletop (Cat: 0x00, Subcats: 0x04, 0x06)
/// </summary>
public sealed class ControlLincViewModel : DeviceViewModel
{
    public ControlLincViewModel(Device d) : base(d)
    {

    }

    static internal bool IsA(Device d)
    {
        return DeviceKind.GetModelType(d.CategoryId, d.SubCategory) == DeviceKind.ModelType.ControlLinc;
    }

    static private int[] SubCats = { 0x04, 0x06 };

    /// <summary>
    /// Operating flags
    /// </summary>

    /// <summary>
    /// Operating flags bits - LEDOn
    /// Read/write (two way bindable), synced with the physical device
    /// </summary>
    public override bool LEDOn
    {
        get => Device.LEDOnTx;
        set
        {
            Debug.Assert(nameof(LEDOn) == nameof(Device.LEDOnTx));
            Device.LEDOnTx = value;
        }
    }

    /// <summary>
    /// Operating flags bits - BeeperOn
    /// Read/write (two way bindable), synced with the physical device
    /// </summary>
    public bool BeeperOn
    {
        get => Device.BeeperOn;
        set
        {
            Debug.Assert(nameof(BeeperOn) == nameof(Device.BeeperOn));
            Device.BeeperOn = value;
        }
    }

    // Override OnDevicePropertyChanged to handle the LEDOnTx property
    protected override void OnDevicePropertyChanged(string propertyName)
    {
        if (propertyName == nameof(Device.LEDOnTx))
            propertyName = nameof(LEDOn);
        base.OnDevicePropertyChanged(propertyName);
    }
}
