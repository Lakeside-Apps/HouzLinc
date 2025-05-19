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

using System.Diagnostics;
using Microsoft.UI.Dispatching;

namespace Common;

/// <summary>
/// A class to schedule and run jobs on the UI thread
/// 
/// Jobs:
/// Jobs can be added to the queue of the scheduler.
/// Jobs have a description, a handler, a completion callback, delay and priority and can be part of a group.
/// Once the given delay after a job was queued has elapsed, the job will be run after other jobs in front of
/// it in the queue of same priority or higher, but before any job of lower priority.
/// High pri jobs can be run even if a medium or low pri job is already running.
/// Jobs are run by invoking their handler, which can be async or not. Handlers return a single value of a
/// declared type. In the absence of a completion callback, a bool return value indicates success or not.
/// An int, float or double indicates success if >= 0 and a reference to an object indicates success if not null.
/// Alternatively, success can be determined by the completion callback, if provided, which takes the returned
/// value from the job hander as input and returns a bool indicating success or not.
/// 
/// Automatic retries:
/// A max number of retries can be specified for a job. In that case, if a completion callback is provided, it will
/// be invoked after every try, until either success is returned or the max number of retries is reached. Other
/// jobs can be running between two retries of a given job.
///
/// Groups:
/// Jobs can be part of groups.
/// Groups will call their own completion callback, if supplied, once all jobs within them have completed.
/// The success parameter of the completion callback will be false if any of the jobs in the group failed.
/// Groups can be cancelled, which has the effect of cancelling all jobs still pending in that group.
/// Groups can be nested.
///
/// Step handlers:
/// Jobs can have a step handler instead of a regular handler. This allows to create interrupatable jobs, i.e.,
/// jobs that can be interrupted by other jobs of higher priority level in between steps. A step handler is first
/// called with its bool isFirstStep parameter true and repeatedly afterwards with that same parameter false,
/// until it returns true in the "completed" field of its returned tuple.
/// </summary>
public abstract class Scheduler
{
    /// <summary>
    /// Job pre-handler
    /// Performs any pre-job handling and can cancel the job and all associated logging.
    /// <returns>false to cancel the job</returns>
    public delegate Task<bool> JobPrehandlerAsync();
    public delegate bool JobPrehandler();

    /// <summary>
    /// Job handler
    /// Perforrms the job
    /// </summary>
    /// <typeparam name="RetType">Handler return value</typeparam>
    /// <returns></returns>
    public delegate Task<RetType> JobHandlerAsync<RetType>();
    public delegate RetType JobHandler<RetType>();

    /// <summary>
    /// Step handler
    /// Performs one step of the job and returns whether the job is completed or not
    /// </summary>
    /// <param name="retType">Step return value</param>
    /// <param name="completed">true if the job is done</param>
    /// <returns></returns>
    public delegate Task<(RetType retType, bool completed)> JobStepHandlerAsync<RetType>(bool isFirstStep);
    public delegate (RetType retType, bool completed) JobStepHandler<RetType>(bool isFirstStep);

    /// <summary>
    /// Completion callback
    /// Called after the job completed
    /// </summary>
    /// <param name="retType">Success if RetType is bool, job return value otherwise</param>
    public delegate void JobCompletionCallback<RetType>(RetType retType);

    // Cancellation handler
    public delegate Task<bool> JobCancellationHandlerAsync();

    /// <summary>
    /// Priority at which to run the job. Jobs of higher priority are handled first.
    /// </summary>
    public enum Priority
    {
        Low,        // job runs when no other jobs are scheduled or running , and its delay has expired
        Medium,     // Job runs when no high priority jobs are scheduled or running, and its delay has expired
        High        // job runs as soon as its delay has expired, even if medium or low job is running already
    }

    /// <summary>
    /// Specifies logging mode for a job
    /// </summary>
    public enum LogLevel
    {
        Normal,     // log normally
        DebugOnly,  // log to debug only
        Quiet       // don't log anything
    }

    /// <summary>
    /// Base class for a job or a group in the scheduler
    /// </summary>
    class JobBase
    {
        public JobBase(Scheduler scheduler, string description, object? group, LogLevel logLevel)
        {
            this.scheduler = scheduler;
            this.Description = description;
            this.logLevel = logLevel;
            this.group = group as Group;
            this.group?.AddJob(this);
        }

        protected Scheduler scheduler;  // Scheduler this job or group belongs to
        protected Group? group;         // Group this job or group is part of (null if not part of a group)
        protected LogLevel logLevel;    // Logging level for this job
        protected bool isCancelling;    // A request to cancel this job has been issued

        /// <summary>
        /// Job description/description
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Whether this jobBase is part of a given group
        /// </summary>
        /// <param name="group"></param>
        /// <returns>true if this job part of the specificed group</returns>
        public bool IsPartOfGroup(object group)
        {
            Group? g = this.group;
            while (g != null)
            {
                if (g == group) return true;
                g = g.group;
            }
            return false;
        }

        /// <summary>
        /// Whether this jobBase is part of a cancelled group
        /// </summary>
        /// <returns></returns>
        public bool IsPartOfCancellingGroup()
        {
            Group? g = this.group;
            while (g != null)
            {
                if (g.isCancelling) return true;
                g = g.group;
            }
            return false;
        }

        /// <summary>
        /// Whether this job is pending, i.e., its handler will be run at least once in the future
        /// </summary>
        public bool IsPending { get; protected private set; }

        /// <summary>
        /// Whether this job has fully completed, including all steps and retries
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Whether this job has been cancelled
        /// </summary>
        public bool IsCancelled { get; private set; }

        /// <summary>
        /// Notify this job it has completed and has been removed from the queue
        /// </summary>
        /// <param name="success"></param>
        public void NotifyCompleted(bool success)
        {
            if (success)
            {
                LogCompleted($"{Description} - Done");
            }
            else
            {
                LogFailed($"{Description} - Failed");
            }
            group?.NotifyJobCompleted(success);
            IsCompleted = true;
        }

        /// <summary>
        /// Notify this job it is being cancelled
        /// </summary>
        public void NotifyCancelling()
        {
            isCancelling = true;
        }

        /// <summary>
        /// Notify this job it has been cancelled and has been removed from the queue
        /// </summary>
        public void NotifyCancelled()
        {
            IsCancelled = true;

            // If part of a group that is being cancelled, let the group log cancellation
            if (!IsPartOfCancellingGroup())
            {
                LogCompleted($"{Description} - Cancelled");
            }
            group?.NotifyJobCancelled();
        }

        protected void LogRunning(string msg)
        {
            switch(logLevel)
            {
                case LogLevel.Normal:
                {
                    scheduler.LogRunning(msg);
                    break;
                }
                case LogLevel.DebugOnly:
                {
                    scheduler.LogDebug(msg);
                    break;
                }
            }
        }

        protected void LogCompleted(string msg)
        {
            switch (logLevel)
            {
                case LogLevel.Normal:
                {
                    scheduler.LogCompleted(msg);
                    break;
                }
                case LogLevel.DebugOnly:
                {
                    scheduler.LogDebug(msg);
                    break;
                }
            }
        }

        protected void LogFailed(string msg)
        {
            switch (logLevel)
            {
                case LogLevel.Normal:
                {
                    scheduler.LogFailed(msg);
                    break;
                }
                case LogLevel.DebugOnly:
                {
                    scheduler.LogDebug(msg);
                    break;
                }
            }
        }

        protected void LogDebug(string msg)
        {
            if (logLevel != LogLevel.Quiet)
            {
                scheduler.LogDebug(msg);
            }
        }
    }

    /// <summary>
    /// Group of jobs
    /// </summary>
    class Group : JobBase
    {
        public Group(Scheduler scheduler, string description, object? group, JobCompletionCallback<bool>? jobCompletionCallback, LogLevel logLevel) : base(scheduler, description, group, logLevel)
        {
            this.jobCompletionCallback = jobCompletionCallback;
        }

        private int pendingJobs = 0;                                // number of not yet completed jobs in the group
        private int completedJobs = 0;                              // number of jobs completed so far
        private int cancelledJobs = 0;                              // number of jobs cancelled so far
        private int errorCount = 0;                                 // number of jobs having reported an error 
        private JobCompletionCallback<bool>? jobCompletionCallback; // callback when the group is completed

        /// <summary>
        /// Add a job to the group
        /// </summary>
        public void AddJob(JobBase job)
        {
            if (IsCompleted || IsCancelled)
            {
                LogDebug($"Attempting to add a job to an already completed or cancelled group ({Description})!");
                Debug.Assert(false);
                return;
            }
            pendingJobs++;
            LogDebug($"Adding job {job.Description} to group {Description}, which now has {pendingJobs} pending jobs");
        }

        /// <summary>
        /// Notify group that a job of the group has started
        /// </summary>
        public void NotifyJobStarting()
        {
            if (completedJobs == 0)
            {
                // Starting first job, if group is part of another group, notify it
                if (group != null)
                {
                    group.NotifyJobStarting();
                }
                LogRunning(Description);
            }
        }

        /// <summary>
        /// Notify this group that a job has completed
        /// If the last job in the group has completed, this will invoke the group completion handler 
        /// and remove the group from the scheduler queue
        /// </summary>
        /// <param name="success"></param>
        public void NotifyJobCompleted(bool success)
        {
            if (pendingJobs > 0)
            {
                pendingJobs--;
                completedJobs++;
                if (!success)
                {
                    errorCount++;
                }

                CompleteIfAllJobsCompletedOrCancelled();
            }
        }

        /// <summary>
        /// Notify this group that a job has been cancelled
        /// Similar to NotifyJobCompleted
        /// </summary>
        public void NotifyJobCancelled()
        {
            if (pendingJobs > 0)
            {
                pendingJobs--;
                cancelledJobs++;

                CompleteIfAllJobsCompletedOrCancelled();
            }
        }

        /// <summary>
        /// Return whether the group has pending members
        /// </summary>
        /// <returns></returns>
        public bool HasPendingJobs()
        {
            return pendingJobs > 0;
        }

        /// <summary>
        /// "Schedule" this group. 
        /// By itself this does not do anything other than adding the group to the scheduler queue
        /// </summary>
        public void Schedule()
        {
            if (IsCompleted || IsCancelled)
            {
                LogDebug("Attempting to schedule an already completed or cancelled group ({Description})!");
                Debug.Assert(false);
                return;
            }
            LogDebug($"Scheduling Group: {Description}");
            scheduler.jobs.Add(this);
        }

        /// <summary>
        /// Complete this group if all jobs have completed or been cancelled
        /// </summary>
        private void CompleteIfAllJobsCompletedOrCancelled()
        {
            LogDebug($"Group {Description} completed {completedJobs} jobs, with {errorCount} errors, and now has {pendingJobs} pending jobs");

            if (pendingJobs == 0)
            {
                bool success = errorCount == 0;

                scheduler.jobs.Remove(this);

                if (isCancelling)
                {
                    LogDebug($"Group Cancelled: {Description} after completing {completedJobs} job(s) {(errorCount == 0 ? "successfully" : $"with {errorCount} errors")}");
                    NotifyCancelled();
                }
                else
                {
                    LogDebug($"Group Completed: {Description} after completing {completedJobs} job(s) {(errorCount == 0 ? "successfully" : $"with {errorCount} errors")}");
                    NotifyCompleted(success);
                }

                // TODO: should we call the completion callback when the group is cancelled?
                jobCompletionCallback?.Invoke(success);
            }
        }
    }

    /// <summary>
    /// Base class for a job in the job scheduler
    /// </summary>
    abstract class Job : JobBase
    {
        public Job(Scheduler scheduler, string description, Group? group, TimeSpan delay, Priority priority, int maxRunCount, LogLevel logLevel) : base(scheduler, description, group, logLevel)
        {
            this.Delay = delay;
            this.Priority = priority;
            this.maxRunCount = maxRunCount;
        }

        // Automatic reruns
        protected int runCount;                                 // number of times this job has run
        protected int maxRunCount = 3;                          // max number of times to run this job
        protected TimeSpan rerunDelay = TimeSpan.FromSeconds(5);// delay between reruns

        // Stepped jobs
        protected bool isContinuationStep;                      // this job is a continuation step
        protected int stepCount;                                // number of steps this job has run

        /// <summary>
        /// Delay between job scheduled and job starting
        /// </summary>
        public TimeSpan Delay { get; internal set; }

        /// <summary>
        /// Priority of this job
        /// </summary>
        public Priority Priority { get; private set; }

        /// <summary>
        /// Priority expressed as a string for logging
        /// </summary>
        public string PriorityAsString => Priority == Priority.High ? "high" : (Priority == Priority.Medium ? "medium" : "low");

        /// <summary>
        /// Time at which this Job is ready to run
        /// </summary>
        public DateTime ReadyTime;

        /// <summary>
        /// Wether job is ready to run (whether delay has expired)
        /// </summary>
        public bool IsReady => DateTime.Now >= ReadyTime;

        /// <summary>
        /// Whether this job is run in steps with a step handler
        /// </summary>
        public virtual bool IsSteppedJob => false;

        /// <summary>
        /// Schedule this job to start after its delay is expired
        /// </summary>
        public void Schedule()
        {
            scheduler.Stop();
            if (isContinuationStep)
            {
                // Schedule continuation steps at front of queue, to run ahead of jobs of same priority or lower
                LogDebug($"Scheduling {(runCount > 1 ? "Step Retry:" : "Next Step:")} {Description} at {PriorityAsString} priority");
                scheduler.jobs.Insert(0, this);
                ReadyTime = DateTime.Now;
            }
            else
            {
                // Schedule regular jobs at end of queue to run after all jobs of same priority or higher
                LogDebug($"Scheduling {(runCount > 1 ? " Retry:" : ":")} {Description} to run in {Delay.ToString()} at {PriorityAsString} priority");
                IsPending = true;
                scheduler.jobs.Add(this);
                ReadyTime = DateTime.Now + Delay;
            }
            scheduler.Start();
        }

        /// <summary>
        /// Schedule this job to start after a given delay
        /// </summary>
        /// <param description="delay"></param>
        public void Schedule(TimeSpan delay)
        {
            this.Delay = delay;
            Schedule();
        }

        /// <summary>
        /// Notify this stepped job, one of its step has completed and has been removed from the queue
        /// </summary>
        public void NotifyStepCompleted()
        {
            LogDebug($"Step {stepCount + 1} Completed: {Description}");
        }


        /// <summary>
        /// Log that this job is running
        /// </summary>
        public void LogRunning()
        {
            if (IsSteppedJob)
            {
                LogRunning($"{Description}, Step {stepCount + 1}{(logLevel == LogLevel.DebugOnly ? $", {PriorityAsString} priority" : "")}{(runCount > 1 ? $" (Attempt {runCount})" : "")}");
            }
            else
            {
                LogRunning($"{Description}{(logLevel == LogLevel.DebugOnly ? $", {PriorityAsString} priority" : "")}{(runCount > 1 ? $" (Attempt {runCount})" : "")}");
            }
        }
    }

    /// <summary>
    /// Synchronously run job
    /// </summary>
    abstract class SyncJobBase : Job
    {
        public SyncJobBase(Scheduler scheduler, string description, Group? group, TimeSpan delay, Priority priority, int maxRunCount, LogLevel logLevel)
            : base(scheduler, description, group, delay, priority, maxRunCount, logLevel)
        { }

        public abstract void Run();
    }

    class SyncJob<RetType> : SyncJobBase
    {
        public SyncJob(Scheduler scheduler, string description, Group? group, JobHandler<RetType>? jobHandler, 
            JobStepHandler<RetType>? jobStepHandler, JobCompletionCallback<RetType>? jobCompletionCallback,
            JobPrehandler? jobPrehandler, TimeSpan delay, Priority priority, int maxRunCount, LogLevel logLevel)
            : base(scheduler, description, group, delay, priority, maxRunCount, logLevel)
        {
            this.jobPrehandler = jobPrehandler;
            this.jobHandler = jobHandler;
            this.jobStepHandler = jobStepHandler;
            this.jobCompletionCallback = jobCompletionCallback;
        }

        /// <summary>
        /// Run this job
        /// </summary>
        public override void Run()
        {
            runCount++;

            // If part of a group and not a continuation step,
            // notify the group that a job is starting
            if (group != null && !isContinuationStep)
            {
                group.NotifyJobStarting();
            }

            RetType retValue;
            bool completed = false;
            IsPending = false;
            LogRunning();

            if (jobStepHandler != null)
            {
                // If this is a stepped job, run the step handler
                (retValue, completed) = jobStepHandler.Invoke(!isContinuationStep);
            }
            else if (jobHandler != null)
            {
                // Otherwise run the regular handler
                retValue = jobHandler.Invoke();
                completed = true;
            }
            else
            {
                throw new InvalidOperationException("Scheduler: both jobHandler and jobStepHandler are null");
            }

            // Device success from the returned value of the handler
            bool success = EvaluateSuccess<RetType>(retValue);

            // If job failed and is not cancelled, reschedule up to maxRunCount runs
            if (!success)
            {
                if (runCount < maxRunCount && !isCancelling)
                {
                    Schedule(rerunDelay);
                    completed = false;
                }
                else
                {
                    completed = true;
                }
            }

            if (success && !completed && !isCancelling && jobStepHandler != null)
            {
                // If success and we need to run a continuation step, schedule it now
                NotifyStepCompleted();
                runCount = 0;
                isContinuationStep = true;
                stepCount++;
                Schedule();
            }

            // Log completion/cancellation, set state and notify group if part of one
            if (completed)
            {
                NotifyCompleted(success);
                jobCompletionCallback?.Invoke(retValue);
            }
            else if (isCancelling)
            {
                NotifyCancelled();
            }
        }

        /// <summary>
        /// Whether this job is run in steps
        /// </summary>
        public override bool IsSteppedJob => jobStepHandler != null;

        /// <summary>
        /// Determine if a this job is same or better (i.e., lower delay, higher pri) as the passed parameters
        /// </summary>
        /// <param name="jobHandler"></param>
        /// <param name="jobCompletionCallback"></param>
        /// <param name="delay"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public bool IsSameOrBetterThan(JobHandler<RetType> jobHandler, JobCompletionCallback<RetType> jobCompletionCallback, TimeSpan delay, Priority priority)
        {
            return 
                jobHandler.Equals(this.jobHandler) &&
                ((jobCompletionCallback == null && this.jobCompletionCallback == null) || (jobCompletionCallback != null && jobCompletionCallback.Equals(this.jobCompletionCallback))) &&
                delay.CompareTo(this.Delay) >= 0 && 
                priority <= this.Priority &&
                this.isContinuationStep == false;
        }

        private JobHandler<RetType>? jobHandler;                        // Job handler (for non-interruptable jobs)
        private JobStepHandler<RetType>? jobStepHandler;                // Job step handler (for interruptable jobs)
        private JobCompletionCallback<RetType>? jobCompletionCallback;  // callback when the job is completed
        private JobPrehandler? jobPrehandler;                           // called before job handler and can cancel job
    }

    /// <summary>
    /// Asynchronously run job
    /// </summary>
    abstract class AsyncJobBase : Job
    {
        public AsyncJobBase(Scheduler scheduler, string description, Group? group, TimeSpan delay, Priority priority, int maxRunCount, LogLevel logLevel)
            : base(scheduler, description, group, delay, priority, maxRunCount, logLevel)
        { }

        public abstract Task RunAsync();
    }

    class AsyncJob<RetType> : AsyncJobBase
    {
        public AsyncJob(Scheduler scheduler, string description, Group? group, JobHandlerAsync<RetType>? jobHandlerAsync, 
            JobStepHandlerAsync<RetType>? jobStepHandlerAsync, JobCompletionCallback<RetType>? jobCompletionCallback, 
            JobPrehandlerAsync? jobPrehandlerAsync, TimeSpan delay, Priority priority, int maxRunCount, LogLevel logLevel)
            : base(scheduler, description, group, delay, priority, maxRunCount, logLevel)
        {
            this.jobHandlerAsync = jobHandlerAsync;
            this.jobStepHandlerAsync = jobStepHandlerAsync;
            this.jobCompletionCallback = jobCompletionCallback;
            this.jobPrehandlerAsync = jobPrehandlerAsync;
        }

        /// <summary>
        /// Run this job
        /// </summary>
        public override async Task RunAsync()
        {
            runCount++;

            // If prehandler is provided, call it and cancel job if it returns false
            if (jobPrehandlerAsync != null && !await jobPrehandlerAsync.Invoke())
            {
                LogDebug($"Prehandler cancelled job {Description}");
                return;
            }

            // If part of a group and not a continuation step,
            // notify the group that a job is starting
            if (group != null && !isContinuationStep)
            {
                group.NotifyJobStarting();
            }

            RetType retValue;
            bool completed = false;

            IsPending = false;
            LogRunning();

            if (jobStepHandlerAsync != null)
            {
                // If this is a stepped job, run the step handler
                (retValue, completed) = await jobStepHandlerAsync.Invoke(!isContinuationStep);
            }
            else if (jobHandlerAsync != null)
            {
                // Otherwise run the regular handler
                retValue = await jobHandlerAsync.Invoke();
                completed = true;
            }
            else
            {
                throw new InvalidOperationException("Scheduler: both jobHandlerAsync and jobStepHandlerAsync are null");
            }

            // If we have completed the job and there is a completion callback, it will tell us whether we have success
            // otherwise compute it from the returned value of the handler
            bool success = EvaluateSuccess<RetType>(retValue);

            // If job failed and is not cancelled, reschedule up to maxRunCount runs
            if (!success)
            {
                if (runCount < maxRunCount && !isCancelling)
                {
                    Schedule(rerunDelay);
                    completed = false;
                }
                else
                {
                    completed = true;
                }
            }

            if (success && !completed && !isCancelling && jobStepHandlerAsync != null)
            {
                // If success and we need to run a continuation step, schedule it now
                NotifyStepCompleted();
                runCount = 0;
                isContinuationStep = true;
                stepCount++;
                Schedule();
            }

            // Log completion/cancellation, set state and notify group if part of one
            if (completed)
            {
                NotifyCompleted(success);
                jobCompletionCallback?.Invoke(retValue);
            }
            else if (isCancelling)
            {
                NotifyCancelled();
            }
        }

        /// <summary>
        /// Whether this job is run in steps
        /// </summary>
        public override bool IsSteppedJob => jobStepHandlerAsync != null;

        private JobHandlerAsync<RetType>? jobHandlerAsync;                          // job handler (for uninteruptable jobs only)
        private JobStepHandlerAsync<RetType>? jobStepHandlerAsync;                  // Job step handler (for interuptable, stepped jobs)
        private JobCompletionCallback<RetType>? jobCompletionCallback;              // callback when the job is completed
        private JobPrehandlerAsync? jobPrehandlerAsync;                             // called before job handler and can cancel job
    }


    /// <summary>
    /// Add a group to the queue of the scheduler
    /// By themselves groups do not do anything, but they provide a mechanism for multiple jobs to be groupped
    /// Groups hold a completion callback that is invoked when all jobs in the group have completed
    /// A group will be removed from the queue when all job in the group have completed
    /// </summary>
    /// <param name="description"></param>
    /// <param name="completionCallback"></param>
    /// <param name="group"></param>
    /// <returns></returns>
    public object AddGroup(string description, JobCompletionCallback<bool>? completionCallback = null, object? group = null, LogLevel logLevel = LogLevel.Normal)
    {
        Group newGroup = new Group(this, description, group as Group, completionCallback, logLevel);
        newGroup.Schedule();
        return newGroup;
    }

    /// <summary>
    /// Add a job in the queue of the scheduler. 
    /// If an equivalent job is already pending in the queue, not job is added 
    /// and the returned object corresponds to the existing job.
    /// </summary>
    /// <param description="description">Job description</param>
    /// <param description="handler">Handler executing the job</param>
    /// <param description="stepHandler">step handler for interruptable (stepped) jobs</param>
    /// <param description="completionCallback">Executed when the job has completed</param>
    /// <param description="delay">Delay before starting the job</param>
    /// <param description="priority">high, medium, low</param>
    /// <param name="group">group this job is part of, if any</param>
    /// <returns>Object identifying the new job</returns>
    public object AddJob<RetType>(string description, JobHandler<RetType> handler, JobCompletionCallback<RetType>? completionCallback = null,
        JobPrehandler? prehandler = null,object? group = null, 
        TimeSpan delay = new TimeSpan(), Priority priority = Priority.Medium, int maxRunCount = 3, LogLevel logLevel = LogLevel.Normal)
    {
        SyncJob<RetType> job = new SyncJob<RetType>(this, description, group as Group, handler, null, completionCallback, prehandler, delay, priority, maxRunCount, logLevel);
        job.Schedule();
        return job;
    }
    public object AddSteppedJob<RetType>(string description, JobStepHandler<RetType> stepHandler, JobCompletionCallback<RetType>? completionCallback = null,
        JobPrehandler? prehandler = null, object? group = null, 
        TimeSpan delay = new TimeSpan(), Priority priority = Priority.Medium, int maxRunCount = 3, LogLevel logLevel = LogLevel.Normal)
    {
        SyncJob<RetType> job = new SyncJob<RetType>(this, description, group as Group, null, stepHandler, completionCallback, prehandler, delay, priority, maxRunCount, logLevel);
        job.Schedule();
        return job;
    }

    /// <summary>
    /// Wait for a minumum duration and until any currently running job at that time has completed
    /// </summary>
    /// <param name="completionCallback"></param>
    /// <param name="delay"></param>
    /// <returns></returns>
    public object Wait(JobCompletionCallback<bool> completionCallback, TimeSpan delay, int maxRunCount = 3, LogLevel logLevel = LogLevel.Normal)
    {
        return AddJob("Wait " + delay.ToString(), () => { return true; }, completionCallback, prehandler: null, group: null, delay, Priority.Low, maxRunCount, logLevel);
    }

    /// <summary>
    /// Add an async job in the queue of the scheduler. 
    /// If an equivalent job is already pending in the queue, not job is added 
    /// and the returned object corresponds to the existing job.
    /// </summary>
    /// <param description="description">Job description</param>
    /// <param description="handlerAsync">Handler executing the job</param>
    /// <param description="stepHandlerAsync">step handler for interruptable (stepped) jobs</param>
    /// <param description="completionCallback">Executed when the job has completed</param>
    /// <param description="delay">Delay before starting the job</param>
    /// <param description="priority">high, medium, low</param>
    /// <param name="group">group this job is part of, if any</param>
    /// <returns>Object identifying the new job</returns>
    public object AddAsyncJob<RetType>(string description, JobHandlerAsync<RetType> handlerAsync, JobCompletionCallback<RetType>? completionCallback = null,
        JobPrehandlerAsync? prehandlerAsync = null, object ? group = null, 
        TimeSpan delay = new TimeSpan(), Priority priority = Priority.Medium, int maxRunCount = 3, LogLevel logLevel = LogLevel.Normal
        )
    {
        AsyncJob<RetType> job = new AsyncJob<RetType>(this, description, group as Group, handlerAsync, null, completionCallback, prehandlerAsync, delay, priority, maxRunCount, logLevel);
        job.Schedule();
        return job;
    }

    public object AddAsyncSteppedJob<RetType>(string description, JobStepHandlerAsync<RetType> stepHandlerAsync, JobCompletionCallback<RetType>? completionCallback = null,
        JobPrehandlerAsync? prehandlerAsync = null, object? group = null, 
        TimeSpan delay = new TimeSpan(), Priority priority = Priority.Medium, int maxRunCount = 3, LogLevel logLevel = LogLevel.Normal
        )
    {
        AsyncJob<RetType> job = new AsyncJob<RetType>(this, description, group as Group, null, stepHandlerAsync, completionCallback, prehandlerAsync, delay, priority, maxRunCount, logLevel);
        job.Schedule();
        return job;
    }

    /// <summary>
    /// Cancel a pending job that is not already running or a group
    /// </summary>
    /// <param description="obj">The job object returned by AddJob.</param>
    /// <returns>true if the job was pending and got cancelled, false if 
    /// - the job is currently running or is a group with a job currently running, in which case only remaining retries are cancelled
    /// - the job has already completed or been cancelled, in which case this method does nothing
    /// This method always return true for a group and does its best at cancelling call jobs in the group
    /// </returns>
    public bool CancelJob(object? obj, JobCancellationHandlerAsync? cancellationHandler = null)
    {
        bool success = true;

        if (obj != null)
        {
            Stop();

            switch (obj)
            {
                case Job job:
                {
                    success = CancelJobHelper(job, cancellationHandler);
                    break;
                }

                case Group group:
                {
                    if (group.HasPendingJobs())
                    {
                        group.NotifyCancelling();

                        // Cancel all jobs in the given group and any group it contains
                        // Including the running job
                        List<Job> jobsToCancel = new List<Job>();

                        {
                            var job = runningJob ?? runningHighPriJob;
                            if (job != null && job.IsPartOfGroup(group))
                            {
                                jobsToCancel.Add(job);
                            }
                        }

                        foreach (JobBase jobBase in jobs)
                        {
                            if (jobBase.IsPartOfGroup(group))
                            {
                                if (jobBase is Job job)
                                {
                                    jobsToCancel.Add(job);
                                }
                            }
                        }

                        foreach (Job job in jobsToCancel)
                        {
                            // Ignore return value, we consider the group cancelled regardless
                            // of whether some jobs are left in it or not
                            if (!CancelJobHelper(job, cancellationHandler))
                            {
                                success = false;
                            }
                        }
                    }

                    break;
                }
            }

            Start();
            LogRunningCont();
        }

        return success;
    }

    // Private helper for routine above
    // Cancel the job if has not run yet, and if it has, suppresses any retry
    // Return true only if job was cancelled before running
    private bool CancelJobHelper(Job job, JobCancellationHandlerAsync? cancellationHandler)
    {
        bool success = false;

        if (job != runningJob && job != runningHighPriJob)
        {
            if (jobs.Remove(job))
            {
                job.NotifyCancelled();
                success = true;
            }
        }
        else
        {
            // Supresses any retry
            job.NotifyCancelling();

            // If we have an async cancellation handler, call it so that it can perform
            // whatever is necessary to cancel the running job
            if (cancellationHandler != null)
            {
                DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                dispatcherQueue.TryEnqueue(async () => await cancellationHandler());
            }
        }

        return success;
    }

    /// <summary>
    /// Returns whether a job is pending, i.e., its handler will be called at least once in the future
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool IsPending(object? obj)
    {
        return (obj as JobBase)?.IsPending ?? false;
    }

    /// <summary>
    /// Returns whether a job has been cancelled
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool IsCancelled(object? obj)
    {
        return (obj as JobBase)?.IsCancelled ?? false;
    }

    /// <summary>
    /// Returns the priority of a given job
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Priority GetPriority(object obj)
    {
        return (obj as Job)!.Priority;
    }

    /// <summary>
    /// Reduces the delay of a pending job to zero
    /// with the effect of starting the job as soon as other in front of it have completed.
    /// No action if the job is not in the scheduler queue.
    /// </summary>
    /// <param name="obj">Job to start as soon as possible</param>
    public void ClearDelay(object? obj)
    {
        if (obj != null)
        {
            Stop();
            if (obj is Job job)
            {
                job.Delay = TimeSpan.Zero;
                job.ReadyTime = DateTime.Now;
                LogDebug($"Cleared delay of {job.Description}");
                Start();
            }
        }
    }

    // Scheduler name
    protected abstract string GetName();
    
    // Start the scheduler when called as many times as Stop was called
    // Always call Stop() and Start() in pairs
    private void Start()
    {
        Debug.Assert(timer == null);
        Debug.Assert(stopCount > 0);

        stopCount--;

        if (stopCount == 0 && jobs.Count > 0 && !ApplicationType.IsUnitTestFramework)
        {
            Debug.Assert(Environment.CurrentManagedThreadId == SchedulerThreadId);

            var queue = DispatcherQueue.GetForCurrentThread();
            timer = queue.CreateTimer();
            timer.Tick += TimerTick;
            timer.Interval = GetTimerInterval();
            timer.Start();
        }
    }

    // Stop the scheduler until a matching count of calls to Start() occurs
    private void Stop()
    {
        Debug.Assert(stopCount == 0 || timer == null);

        if (stopCount == 0 && timer != null)
        {
            timer.Stop();
            timer = null;
        }

        stopCount++;
    }

    // Compute timer interval (Interval to the soonest to run job)
    private TimeSpan GetTimerInterval()
    {
        DateTime soonestReadyTime = DateTime.MaxValue;
        foreach (JobBase jobBase in jobs)
        {
            if (jobBase is Job)
            {
                if (jobBase is Job job && job.ReadyTime < soonestReadyTime)
                {
                    soonestReadyTime = job.ReadyTime;
                }
            }
        }

        TimeSpan ts = soonestReadyTime - DateTime.Now;
        return (ts > TimeSpan.Zero) ? ts : TimeSpan.Zero;
    }

    // Handler of the timer
    private async void TimerTick(object sender, object e)
    {
        // We should always be called on the UI thread
        Debug.Assert(Environment.CurrentManagedThreadId == SchedulerThreadId);

        Stop();
        await RunAsync();
        Start();
    }

    // Run the scheduler
    // This runs all ready jobs in sequence until the first non-ready job in the queue
    private async Task RunAsync()
    {
        Job? job;
        while ((job = NextJob()) != null)
        {
            jobs.Remove(job);

            if (job.Priority == Priority.High)
            {
                runningHighPriJob = job;
            }
            else
            {
                runningJob = job;
            }

            if (job is AsyncJobBase asyncJob)
            {
                await asyncJob.RunAsync();
            }
            else if (job is SyncJobBase syncJob)
            {
                syncJob.Run();
            }

            if (job.Priority == Priority.High)
            {
                runningHighPriJob = null;
            }
            else
            {
                runningJob = null;
            }
        }
    }

    // Determine next job to run
    private Job? NextJob()
    {
        Job? candidate = null;

        // If a high-pri job is running, nothing else to run
        if (runningHighPriJob == null)
        {
            bool mediumPriJobPending = false;

            foreach (JobBase jobBase in jobs)
            {
                if (!(jobBase is Group))
                {
                    if (jobBase is Job job && job.IsReady)
                    {
                        // If a job is high priority (and not running), it's the next one to run
                        if (job.Priority == Priority.High)
                        {
                            candidate = job;
                            break;
                        }

                        // Otherwise, find the low or medium-pri candidate to run 
                        if (runningJob == null && (candidate == null || job.Priority > candidate.Priority))
                        {
                            // Remember if we have a medium-pri pending job (if not other high pri)
                            if (job.Priority == Priority.Medium)
                            {
                                mediumPriJobPending = true;

                                // And if so, drop any low-pri candidate we might have
                                if (candidate != null && candidate.Priority == Priority.Low)
                                {
                                    candidate = null;
                                }
                            }

                            // We found a candidate if either it is medium-pri or no medium-pri job is pending
                            if (!mediumPriJobPending || job.Priority == Priority.Medium)
                            {
                                candidate = job;
                            }
                        }
                    }
                }
            }
        }

        return candidate;
    }

    // Log async job running continuation after logging concurrent cancellation or other info
    private void LogRunningCont()
    {
        var job = runningJob ?? runningHighPriJob;
        if (job != null)
        {
            job.LogRunning();
        }
    }

    // Derived classes can override to log differently
    protected virtual void LogRunning(string message)
    {
        Logger.Log.Running(message);
    }

    protected virtual void LogCompleted(string message)
    {
        Logger.Log.Completed(message);
    }

    protected virtual void LogFailed(string message)
    {
         Logger.Log.Failed(message);
    }

    protected virtual void LogDebug(string message)
    {
        Logger.Log.Debug(message);
    }

    // Evalute success of the job handler based on return type
    // - bool return value: indicates success or not
    // - int, float and double: zero or positive is success
    // - enum: allways success
    // - object: success when not null.
    static bool EvaluateSuccess<RetType>(RetType retValue)
    {
        switch (retValue)
        {
            case bool:
                return (bool)(object)retValue;
            case int:
                return (int)(object)retValue >= 0;
            case float:
                return (float)(object)retValue >= 0;
            case double:
                return (double)(object)retValue >= 0;
            case Enum:
                return true;
            default:
                return retValue != null;
        }
    }

    // Scheduler timer
    private DispatcherQueueTimer? timer;

    // Pending jobs
    private List<JobBase> jobs = new List<JobBase>();

    // Running low or medium pri job
    private Job? runningJob;

    // Running high pri job (can run over a medium or low pri job)
    private Job? runningHighPriJob;

    // For debug purposes, Id of the thread the scheduler was created on
    int SchedulerThreadId = Environment.CurrentManagedThreadId;

    // Number of times Stop() was called without a matching call to Start()
    int stopCount = 0;

}

