using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Pipes.StateMachine
{
    internal interface IStateMachine<TId, TData>
    {
        State<TId, TData> Handle(State<TId, TData> state, IRequest<TId, TData> request);
    }
}
