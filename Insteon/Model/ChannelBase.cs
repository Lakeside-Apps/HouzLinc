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

namespace Insteon.Model;

public class ChannelBase
{
    public virtual int Id { get; init; }
    public virtual int FollowMask { get; set; }
    public virtual int FollowOffMask { get; set; }
    public virtual ToggleMode ToggleMode { get; set; }
    public virtual bool LEDOn { get; set; }
    public virtual int OnLevel { get; set; }
    public virtual int RampRate { get; set; }
}

public enum ToggleMode
{
    Toggle,
    On,
    Off,
}
