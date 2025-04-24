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
