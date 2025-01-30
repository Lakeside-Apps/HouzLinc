using System;

namespace Common;

/// <summary>
/// This helper class defers execution of an "Executor" method taking one parameter of any type until the end of a block
/// </summary>
/// <typeparam name="T">Type of the parameter</typeparam>

public sealed class DeferredExecution<T> : IDisposable
{
    private readonly Action<T> executor;
    private readonly T param;
    private readonly Action<bool> deferExecution;

    /// <summary>
    /// <param name="executor">Executor method</param>
    /// <param name="param">Parameter to pass to the Executor method</param>
    /// <param name="deferExecutionCallback">callback to start or stop defering the execution. 
    /// This allows the user of this class block execution of the executor when it is deferred.</param>
    /// </summary>
    public DeferredExecution(Action<T> executor, T param, Action<bool> deferExecutionCallback)
    {
        this.executor = executor;
        this.param = param;
        this.deferExecution = deferExecutionCallback;
        deferExecutionCallback(true);
    }

    public void Dispose()
    {
        deferExecution(false);
        executor(param);
    }
}
