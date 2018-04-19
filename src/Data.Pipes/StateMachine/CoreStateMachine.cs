using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Pipes.StateMachine
{
    internal class CoreStateMachine<TId, TData> : IStateMachine<TId, TData>
    {
        public State<TId, TData> Handle(State<TId, TData> state, IRequest<TId, TData> request)
        {
            switch(request)
            {
                default:
                    throw new InvalidOperationException($"Invalid type of {nameof(IRequest<TId, TData>)} ({request.GetType()}) given to state machine");

                case DataSet<TId, TData> data:
                    state.StateMachine = new CacheStateMachine<TId, TData>();
                    state.Results.Add(data.Results);
                    state.Index--;
                    break;

                case Query<TId, TData> query:
                    state.Index++;
                    break;

                case Retry<TId, TData> retry:
                    state.Index--;
                    break;
            }

            return state;
        }
    }
}
