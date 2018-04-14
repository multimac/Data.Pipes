using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sourced.StateMachine;

namespace Sourced
{
    internal struct State<TId, TData>
    {
        public int Counter { get; set; }
        public int Index { get; set; }
        public CancellationToken Token { get; }

        public ConcurrentBag<IReadOnlyDictionary<TId, TData>> Results { get; }
        public IStateMachine<TId, TData> StateMachine { get; set; }

        public State(IStateMachine<TId, TData> machine, CancellationToken token)
        {
            Counter = Index = 0;
            Token = token;

            Results = new ConcurrentBag<IReadOnlyDictionary<TId, TData>>();
            StateMachine = machine;
        }

        public State<TId, TData> Handle(IRequest<TId, TData> request)
            => StateMachine.Handle(this, request);

        public IReadOnlyDictionary<TId, TData> GetResults() => Results
            .SelectMany(r => r.AsEnumerable()).ToDictionary(p => p.Key, p => p.Value);
    }
}
