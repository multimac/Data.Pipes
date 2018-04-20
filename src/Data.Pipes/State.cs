using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data.Pipes.StateMachine;

namespace Data.Pipes
{
    /// <summary>
    /// Tracks the state of a request in the pipeline.
    /// </summary>
    public struct State<TId, TData>
    {
        /// <summary>
        /// The number of times a request has moved around.
        /// </summary>
        public int Counter { get; set; }

        /// <summary>
        /// The index of the stage the request is at.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// A <see cref="CancellationToken"/> used to cancel requests.
        /// </summary>
        public CancellationToken Token { get; }

        /// <summary>
        /// Tracks all results retrieved for a request.
        /// </summary>
        public ConcurrentBag<IReadOnlyDictionary<TId, TData>> Results { get; }

        /// <summary>
        /// The current <see cref="IStateMachine{TId, TData}"/> updating the state of the request.
        /// </summary>
        public IStateMachine<TId, TData> StateMachine { private get; set; }

        /// <summary>
        /// Metadata about the request.
        /// </summary>
        public RequestMetadata Metadata { get; }

        internal State(IStateMachine<TId, TData> machine, RequestMetadata metadata, CancellationToken token)
        {
            Counter = Index = 0;
            Token = token;

            Results = new ConcurrentBag<IReadOnlyDictionary<TId, TData>>();
            StateMachine = machine;
            Metadata = metadata;
        }

        internal State<TId, TData> Handle(IRequest<TId, TData> request)
            => StateMachine.Handle(this, request);

        /// <summary>
        /// Combines all the results in <see cref="State{TId, TData}.Results"/>.
        /// </summary>
        /// <returns>A dictionary containing all the results retrieved for a request.</returns>
        public IReadOnlyDictionary<TId, TData> GetResults() => Results
            .SelectMany(r => r.AsEnumerable()).ToDictionary(p => p.Key, p => p.Value);
    }
}
