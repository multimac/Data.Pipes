using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Pipes.StateMachine
{
    /// <summary>
    /// The default <see cref="IStateMachine{TId, TData}"/> for processing requests.
    /// </summary>
    public class CoreStateMachine<TId, TData> : BaseStateMachine<TId, TData>
    {
        /// <summary>
        /// Constructs a <see cref="CoreStateMachine{TId, TData}"/>.
        /// </summary>
        internal protected CoreStateMachine()
        {
            RegisterRequestHandler<DataSet<TId, TData>>(HandleDataSet);
            RegisterRequestHandler<Query<TId, TData>>(HandleQuery);
            RegisterRequestHandler<Retry<TId, TData>>(HandleRetry);
        }

        private State<TId, TData> HandleDataSet(State<TId, TData> state, DataSet<TId, TData> request)
        {
            state.StateMachine = new CacheStateMachine<TId, TData>();
            state.Results.Add(request.Results);
            state.Index--;

            return state;
        }

        private State<TId, TData> HandleQuery(State<TId, TData> state, Query<TId, TData> request)
        {
            state.Index++;

            return state;
        }

        private State<TId, TData> HandleRetry(State<TId, TData> state, Retry<TId, TData> request)
        {
            state.Index--;

            return state;
        }
    }
}
