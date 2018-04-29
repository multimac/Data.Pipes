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
    public class CacheStateMachine<TId, TData> : BaseStateMachine<TId, TData>
    {
        /// <summary>
        /// Constructs a <see cref="CacheStateMachine{TId, TData}"/>.
        /// </summary>
        internal protected CacheStateMachine()
        {
            RegisterRequestHandler<DataSet<TId, TData>>(HandleDataSet);
        }

        private State<TId, TData> HandleDataSet(State<TId, TData> state, DataSet<TId, TData> request)
        {
            state.Index--;

            return state;
        }
    }
}
