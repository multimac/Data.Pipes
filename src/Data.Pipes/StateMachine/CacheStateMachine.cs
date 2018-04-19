using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Pipes.StateMachine
{
    /// <summary>
    /// The <see cref="IStateMachine{TId, TData}"/> used to handle propagating
    /// <see cref="DataSet{TId, TData}"/> requests back through a pipeline.
    /// </summary>
    internal class CacheStateMachine<TId, TData> : IStateMachine<TId, TData>
    {
        /// <inheritdoc/>
        public State<TId, TData> Handle(State<TId, TData> state, IRequest<TId, TData> request)
        {
            switch(request)
            {
                default:
                    throw new InvalidOperationException($"Invalid type of {nameof(IRequest<TId, TData>)} ({request.GetType()}) given to state machine");

                case DataSet<TId, TData> data:
                    state.Index--;
                    break;
            }

            return state;
        }
    }
}
