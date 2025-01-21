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

/// <summary>
/// Command to set schedules (timers) to activate/deactive a given group in the Hub
/// NOTE: this command does not work as implemented. Either the syntax is incorrect or my Hub does not support it.
/// </summary>
public sealed class CreateScheduleCommand: HubCommand
{
    public const string Name = "CreateSchedule";
    public const string Help = $@"<group> sunrise|sunset|00:00 sunrise|sunset|00:00 [mon] [tues] [wed] [thurs] [fri] [sat] [sun]";
    private protected override string GetLogName() => Name;
    private protected override string GetLogParams() => string.Empty;

    public CreateScheduleCommand(Gateway gateway,
        byte group,
        string name,
        bool show,
        TimeEventType startTimeType,
        DateTime startTime,
        bool amStart,
        bool pmStart,
        TimeEventType endTimeType,
        DateTime endTime,
        bool amEnd,
        bool pmEnd,
        bool monday,
        bool tuesday,
        bool wednesday,
        bool thursday,
        bool friday,
        bool saturday,
        bool sunday,
        bool cntlUp = false,
        bool cntlOn = false,
        bool cntlOff = false,
        bool cntlDown = false,
        InsteonID? statusDevice = null,
        int statusDeviceGroup = 1,
        bool reportStatus = false,
        bool dimCntlInc = false
    ) : base(gateway)
    {
        this.group = group;
        this.name = name;
        this.show = show;
        this.startTime = startTime;
        this.amStart = amStart;
        this.pmStart = pmStart;
        this.endTime = endTime;
        this.amEnd = amEnd;
        this.pmEnd = pmEnd;
        this.monday = monday;
        this.tuesday = tuesday;
        this.wednesday = wednesday;
        this.thursday = thursday;
        this.friday = friday;
        this.saturday = saturday;
        this.sunday = sunday;
        this.cntlUp = cntlUp;
        this.cntlOn = cntlOn;
        this.cntlOff = cntlOff;
        this.cntlDown = cntlDown;
        this.statusDevice = statusDevice ?? InsteonID.Null;
        this.reportStatus = reportStatus;
        this.dimCntlInc = dimCntlInc;

        IMCommandType = IMCommandTypes.HubConfig;
        IMResponseLength = 3;
        IMCommandParams = $"S{group:D3}={name}=2={tf(show)}" +
            $"={time(startTime)}={tf(amStart)}{tf(pmStart)}" +
            $"={ time(endTime)}={tf(amEnd)}{tf(pmEnd)}" +
            $"={tf(monday)}={tf(tuesday)}={tf(wednesday)}={tf(thursday)}={tf(friday)}={tf(saturday)}={tf(sunday)}" +
            $"={tf(cntlUp)}={tf(cntlOn)}={tf(cntlOff)}={tf(cntlDown)}" +
            $"={statusDevice?.ToCommandString() ?? "AABBCC"}{statusDeviceGroup:X2}={tf(reportStatus)}" +
            $"={tf(dimCntlInc)}{tet(startTimeType)}{tet(endTimeType)}";

        RequestClearBuffer = true;
    }

    // Helper to output t/f for a bool
    private string tf(bool b) => b ? "t" : "f";

    // Helper to time event type
    private string tet(TimeEventType t) => t switch
    {
        TimeEventType.Sunset => "1",
        TimeEventType.Time => "2",
        TimeEventType.Sunrise => "3",
        _ => throw new ArgumentException("Invalid TimeEventType")
    };

    // Heler to output time on a 12 hours cycle with no am/pm designator
    private string time(DateTime t) => t.ToString("hh:mm");

    // Helper to output time on a 12 hours cycle with am/pm designator
    private string userTime(DateTime t, bool am, bool pm) => t.ToString("hh:mm tt");

    public enum TimeEventType : byte
    {
        Sunset,
        Time,
        Sunrise
    }

    private byte group;
    private string name;
    private bool show;
    private DateTime startTime;
    private bool amStart;
    private bool pmStart;
    private DateTime endTime;
    private bool amEnd;
    private bool pmEnd;
    private bool monday;
    private bool tuesday;
    private bool wednesday;
    private bool thursday;
    private bool friday;
    private bool saturday;
    private bool sunday;
    private bool cntlUp;
    private bool cntlOn;
    private bool cntlOff;
    private bool cntlDown;
    private InsteonID statusDevice = InsteonID.Null;
    private bool reportStatus = true;
    private bool dimCntlInc;
    private TimeEventType startTimeType;
    private TimeEventType endTimeType;
}
