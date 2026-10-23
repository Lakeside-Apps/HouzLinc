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

using Insteon.Synchronization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Infrastructure;

[TestClass]
public static class TestAssemblySetup
{
    /// <summary>
    /// Dispatcher of the Insteon scheduler for all tests. Jobs scheduled by the model only run
    /// when a test pumps this dispatcher (RunUntilIdle or AdvanceBy).
    /// </summary>
    internal static TestDispatcher InsteonDispatcher { get; private set; } = null!;

    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        InsteonDispatcher = new TestDispatcher();
        InsteonScheduler.Instance.Dispatcher = InsteonDispatcher;
    }
}
