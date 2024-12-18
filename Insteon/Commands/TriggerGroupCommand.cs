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
using Insteon.Base;

namespace Insteon.Commands;

public sealed class TriggerGroupCommand : DeviceCommand
{
    public const string Name = "TriggerGroup";
    public const string Help = "<DeviceID> <Group> <Level(0-255)>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public TriggerGroupCommand(Gateway gateway, InsteonID deviceID, byte group, byte level, bool usePassedLevel, bool useInstantRamp) : base(gateway, deviceID)
    {
        Command1 = CommandCode_TriggerGroup;
        Command2 = 0;
        SetDataByte(4, CommandCode_LightON);
        SetDataByte(5, 0);
        Group = group;
        Level = level;
        UsePassedLevel = usePassedLevel;
        UseInstantRamp = useInstantRamp;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput(StandardResponseMessage.FromDeviceId.ToString() + ", Group: " + Group.ToString() + ", Level: " + (UsePassedLevel ? Level.ToString() : "local") + (UseInstantRamp ? "Instant Ramp" : ""));
    }

    internal byte Group
    {
        get => DataByte(1);
        private set => SetDataByte(1, value);
    }

    internal byte Level
    {
        get => DataByte(3);
        private set => SetDataByte(3, value);
    }

    internal bool UsePassedLevel
    {
        get => DataByte(2) == 0x01;
        private set => SetDataByte(2, (byte)(value ? 0x01 : 0x00));
    }

    internal bool UseInstantRamp
    {
        get => DataByte(6) == 0x01;
        private set => SetDataByte(6, (byte)(value ? 0x01 : 0x00));
    }
}
