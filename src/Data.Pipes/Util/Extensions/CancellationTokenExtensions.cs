using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Data.Pipes.StateMachine;

namespace Data.Pipes.Util.Extensions
{
    internal static class CancellationTokenExtensions
    {
        public static CancellationTokenAwaiter GetAwaiter(this CancellationToken cancellationToken)
        {
            return new CancellationTokenAwaiter(cancellationToken);
        }

        public class CancellationTokenAwaiter : INotifyCompletion
        {
            private readonly CancellationToken _cancellationToken;

            public CancellationTokenAwaiter(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public void GetResult() { }

            public void OnCompleted(Action action) => _cancellationToken.Register(action);
            public bool IsCompleted => _cancellationToken.IsCancellationRequested;
        }
    }
}
