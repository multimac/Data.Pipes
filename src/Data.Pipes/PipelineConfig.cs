using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data.Pipes.StateMachine;

namespace Data.Pipes
{
    public class PipelineConfig<TId, TData>
    {
        public IStateMachine<TId, TData> InitialStateMachine { get; set; }
            = new CoreStateMachine<TId, TData>();
    }
}
