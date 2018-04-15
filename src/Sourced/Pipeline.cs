using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sourced.StateMachine;

namespace Sourced
{
    public class Pipeline<TId, TData> : IPipeline<TId, TData>
    {
        private readonly ISource<TId, TData> _source;
        private readonly IStage<TId, TData>[] _stages;

        public PipelineMetadata Metadata => PipelineMetadata
            .CreateFromSource<TId, TData>(_source);

        public Pipeline(ISource<TId, TData> source, IEnumerable<IStage<TId, TData>> stages)
            : this(source, stages.ToArray()) { }
        public Pipeline(ISource<TId, TData> source, params IStage<TId, TData>[] stages)
        {
            _source = source;
            _stages = stages;
        }

        public async Task<IReadOnlyDictionary<TId, TData>> GetAsync(IReadOnlyCollection<TId> ids, CancellationToken token = default)
        {
            var machine = new CoreStateMachine<TId, TData>();
            var state = new State<TId, TData>(machine, token);
            var query = new Query<TId, TData>(this, ids);

            await RequestStageAsync(state, query);

            return state.GetResults();
        }

        private async Task ProcessResultAsync(State<TId, TData> state, IRequest<TId, TData> request)
        {
            if (request is Async<TId, TData> asyncRequest)
            {
                try { request = await asyncRequest.Request; }
                catch (OperationCanceledException) { return; }
            }

            await RequestStageAsync(state.Handle(request), request);
        }

        private Task RequestStageAsync(State<TId, TData> state, IRequest<TId, TData> request)
        {
            if (state.Index < -1 || state.Index > _stages.Length)
            {
                throw new IndexOutOfRangeException("Pipeline state has transitioned out-of-bounds.");
            }
            else if (state.Index == _stages.Length)
            {
                return QuerySourceAsync(state, request as Query<TId, TData>);
            }
            else if (state.Index == -1)
            {
                return Task.CompletedTask;
            }

            var stage = _stages[state.Index];
            var requests = stage.Process(request, state.Token);
            var processed = requests.Select(r => ProcessResultAsync(state, r));

            return Task.WhenAll(processed);
        }
        private async Task QuerySourceAsync(State<TId, TData> state, Query<TId, TData> query)
        {
            IReadOnlyDictionary<TId, TData> results;

            try { results = await _source.ReadAsync(query, state.Token); }
            catch (OperationCanceledException) { return; }

            var data = new DataSet<TId, TData>(this, results);
            await RequestStageAsync(state.Handle(data), data);
        }
    }
}
