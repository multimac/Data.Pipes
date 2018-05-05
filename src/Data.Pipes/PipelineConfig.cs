using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data.Pipes.StateMachine;

namespace Data.Pipes
{
    /// <summary>
    /// The configuration options for a <see cref="Pipeline{TId, TData}"/>.
    /// </summary>
    public class PipelineConfig<TId, TData>
    {
        /// <summary>
        /// The <see cref="IStateMachine{TId, TData}"/> to use when a request is first made to the
        /// pipeline.
        /// </summary>
        public IStateMachine<TId, TData> InitialStateMachine
        {
            get => _initialStateMachine ?? DefaultInitialStateMachine;
            set => _initialStateMachine = value;
        }
        private static readonly IStateMachine<TId, TData> DefaultInitialStateMachine = new CoreStateMachine<TId, TData>();
        private IStateMachine<TId, TData> _initialStateMachine;
    }
}
