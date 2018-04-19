using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data.Pipes.StateMachine;

namespace Data.Pipes.Util.Extensions
{
    public static class TaskExtensions
    {
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var delay = Task.Delay(timeout, timeoutCancellationTokenSource.Token);

                if (task != await Task.WhenAny(task, delay))
                    throw new TimeoutException("The operation has timed out.");

                timeoutCancellationTokenSource.Cancel();
                await task;  // Very important in order to propagate exceptions
            }
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var delay = Task.Delay(timeout, timeoutCancellationTokenSource.Token);

                if (task != await Task.WhenAny(task, delay))
                    throw new TimeoutException("The operation has timed out.");

                timeoutCancellationTokenSource.Cancel();
                return await task;  // Very important in order to propagate exceptions
            }
        }
    }
}
