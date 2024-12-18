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

namespace ViewModel.Base;

/// <summary>
/// A class to schedule and execute async UI jobs
/// </summary>
class UIScheduler : Scheduler
{
    internal static UIScheduler Instance = new UIScheduler();

    protected override string GetName()
    {
        return "UI Scheduler";
    }

    // Log job events at Debug/Verbose level only as these logs have little value for the user
    protected override void LogRunning(string message)
    {
        Logger.Log.Debug(GetName() + " - " + message);
    }

    protected override void LogCompleted(string message)
    {
        Logger.Log.Debug(GetName() + " - " + message);
    }

    protected override void LogFailed(string message)
    {
        Logger.Log.Debug(GetName() + " - " + message);
    }
}
