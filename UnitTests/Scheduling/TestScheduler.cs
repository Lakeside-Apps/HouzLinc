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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTests.Infrastructure;

namespace UnitTests.Scheduling;

[TestClass]
public sealed class TestScheduler
{
    private sealed class SchedulerForTest : Scheduler
    {
        protected override string GetName() => "Test Scheduler";
    }

    private TestDispatcher dispatcher = null!;
    private Scheduler scheduler = null!;

    [TestInitialize]
    public void Init()
    {
        dispatcher = new TestDispatcher();
        scheduler = new SchedulerForTest() { Dispatcher = dispatcher };
    }

    [TestMethod]
    public void Job_RunsAndCallsCompletionCallback()
    {
        bool? result = null;
        scheduler.AddJob("Job", () => true, success => result = success);

        Assert.IsNull(result, "Job should not run before the dispatcher runs");
        dispatcher.RunUntilIdle();
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public void Job_WithDelay_RunsOnlyOnceDelayHasElapsed()
    {
        bool ran = false;
        scheduler.AddJob("Delayed job", () => ran = true, delay: TimeSpan.FromSeconds(5));

        dispatcher.AdvanceBy(TimeSpan.FromSeconds(4.9));
        Assert.IsFalse(ran);
        dispatcher.AdvanceBy(TimeSpan.FromSeconds(0.1));
        Assert.IsTrue(ran);
    }

    [TestMethod]
    public void MediumPriorityJob_RunsBeforeEarlierLowPriorityJob()
    {
        var order = new List<string>();
        scheduler.AddJob("Low", () => { order.Add("Low"); return true; }, priority: Scheduler.Priority.Low);
        scheduler.AddJob("Medium", () => { order.Add("Medium"); return true; }, priority: Scheduler.Priority.Medium);

        dispatcher.RunUntilIdle();
        CollectionAssert.AreEqual(new[] { "Medium", "Low" }, order);
    }

    [TestMethod]
    public void FailedJob_IsRetriedUntilMaxRunCount_ThenReportsFailure()
    {
        int runCount = 0;
        bool? result = null;
        scheduler.AddJob("Failing job", () => { runCount++; return false; }, success => result = success, maxRunCount: 3);

        dispatcher.RunUntilIdle();
        Assert.AreEqual(1, runCount);
        Assert.IsNull(result, "No completion until all runs are exhausted");

        // Retries are 5 seconds apart
        dispatcher.AdvanceBy(TimeSpan.FromSeconds(5));
        Assert.AreEqual(2, runCount);
        dispatcher.AdvanceBy(TimeSpan.FromSeconds(5));
        Assert.AreEqual(3, runCount);
        Assert.AreEqual(false, result);

        dispatcher.AdvanceBy(TimeSpan.FromSeconds(30));
        Assert.AreEqual(3, runCount, "No more runs after maxRunCount");
    }

    [TestMethod]
    public void AsyncJob_ContinuationsRunOnDispatcher()
    {
        bool? result = null;
        scheduler.AddAsyncJob("Async job", async () =>
        {
            await Task.Yield();
            return true;
        },
        success => result = success);

        dispatcher.RunUntilIdle();
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public void Group_CompletesAfterAllItsJobs()
    {
        var order = new List<string>();
        var group = scheduler.AddGroup("Group", success => order.Add($"Group {success}"));
        scheduler.AddJob("Job 1", () => { order.Add("Job 1"); return true; }, group: group);
        scheduler.AddJob("Job 2", () => { order.Add("Job 2"); return true; }, group: group);

        dispatcher.RunUntilIdle();
        CollectionAssert.AreEqual(new[] { "Job 1", "Job 2", "Group True" }, order);
    }

    [TestMethod]
    public void Group_ReportsFailure_WhenOneOfItsJobsFails()
    {
        bool? groupResult = null;
        var group = scheduler.AddGroup("Group", success => groupResult = success);
        scheduler.AddJob("Succeeding job", () => true, group: group);
        scheduler.AddJob("Failing job", () => false, group: group, maxRunCount: 1);

        dispatcher.RunUntilIdle();
        Assert.AreEqual(false, groupResult);
    }

    [TestMethod]
    public void CancelledGroup_DoesNotRunItsJobs_ButStillCallsCompletionCallback()
    {
        bool jobRan = false;
        bool groupCompleted = false;
        var group = scheduler.AddGroup("Group", success => groupCompleted = true);
        scheduler.AddJob("Job", () => jobRan = true, group: group);

        scheduler.CancelJob(group);
        dispatcher.RunUntilIdle();

        Assert.IsFalse(jobRan);
        Assert.IsTrue(groupCompleted);
    }

    // Regression test for background device sync stopping: when a job's prehandler declined to run it
    // (e.g., device unreachable), the job vanished without completing, so its group never completed.
    [TestMethod]
    public void AsyncJob_DeclinedByPrehandler_CompletesAsFailure_AndCompletesItsGroup()
    {
        bool handlerRan = false;
        bool? jobResult = null;
        bool? groupResult = null;

        var group = scheduler.AddGroup("Group", success => groupResult = success);
        scheduler.AddAsyncJob("Declined job",
            () => { handlerRan = true; return Task.FromResult(true); },
            success => jobResult = success,
            prehandlerAsync: () => Task.FromResult(false),
            group: group);

        dispatcher.RunUntilIdle();

        Assert.IsFalse(handlerRan, "Handler should not run when the prehandler declines");
        Assert.AreEqual(false, jobResult, "Declined job should complete as a failure");
        Assert.AreEqual(false, groupResult, "Group should complete when its declined job completes");
    }

    [TestMethod]
    public void SteppedAsyncJob_RunsStepsUntilCompleted()
    {
        var steps = new List<bool>();
        bool? result = null;
        scheduler.AddAsyncSteppedJob("Stepped job", isFirstStep =>
        {
            steps.Add(isFirstStep);
            return Task.FromResult((true, steps.Count == 3));
        },
        success => result = success);

        dispatcher.RunUntilIdle();

        CollectionAssert.AreEqual(new[] { true, false, false }, steps, "First step flagged, then two continuation steps");
        Assert.AreEqual(true, result);
    }
}
