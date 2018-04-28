using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data.Pipes.StateMachine;

namespace Data.Pipes
{
    /// <summary>
    /// Represents a pipeline of stages and, ultimately, a source of data. Each
    /// <see cref="IStage{TId, TData}"/> within the pipeline has the ability to alter, drop, or
    /// duplicate requests for data.
    /// </summary>
    public class Pipeline<TId, TData> : IPipeline<TId, TData>
    {
        private readonly PipelineConfig<TId, TData> _config;

        private readonly ISource<TId, TData> _source;
        private readonly IStage<TId, TData>[] _stages;

        /// <inheritdoc/>
        public PipelineMetadata Metadata => PipelineMetadata
            .CreateFromSource<TId, TData>(_source);

        /// <summary>
        /// Constructs a <see cref="Pipeline{TId, TData}"/>.
        /// </summary>
        /// <param name="source">The source at the end of the pipeline.</param>
        /// <param name="stages">A series of stages for requests to pass through.</param>
        public Pipeline(ISource<TId, TData> source, params IStage<TId, TData>[] stages)
            : this(source, new PipelineConfig<TId, TData>(), stages) { }

        /// <summary>
        /// Constructs a <see cref="Pipeline{TId, TData}"/>.
        /// </summary>
        /// <param name="source">The source at the end of the pipeline.</param>
        /// <param name="config">The configuration for the pipeline.</param>
        /// <param name="stages">A series of stages for requests to pass through.</param>
        public Pipeline(ISource<TId, TData> source, PipelineConfig<TId, TData> config, params IStage<TId, TData>[] stages)
        {
            _config = config;
            _source = source;
            _stages = stages;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyDictionary<TId, TData>> GetAsync(IReadOnlyCollection<TId> ids, CancellationToken token = default)
        {
            var machine = _config.InitialStateMachine;
            var metadata = new RequestMetadata(Metadata);
            var state = new State<TId, TData>(machine, metadata, token);

            try
            {
                await Task.Run(() => ProcessPipelineAsync(state, ids));
            }
            catch (AggregateException ex)
            {
                throw new PipelineException<TId, TData>(state.GetResults(), ex.Flatten().InnerExceptions);
            }
            catch (Exception ex)
            {
                throw new PipelineException<TId, TData>(state.GetResults(), ex);
            }

            return state.GetResults();
        }
        private async Task ProcessPipelineAsync(State<TId, TData> state, IReadOnlyCollection<TId> ids)
        {
            await RequestStageAsync(state, new Query<TId, TData>(state.Metadata, ids));
            await SignalStagesAsync(state, new PipelineComplete<TId, TData>(state.Metadata));
        }

        private async Task ProcessRequestBatchAsync(State<TId, TData> state, IEnumerable<IRequest<TId, TData>> requests)
        {
            var collected = new List<IRequest<TId, TData>>();
            var exceptions = new List<Exception>();

            try
            {
                foreach (var request in requests)
                    collected.Add(request);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == state.Token) { }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            var processed = collected.Select(request => ProcessRequestAsync(state, request));

            try
            {
                await Task.WhenAll(processed);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }
        private async Task ProcessRequestAsync(State<TId, TData> state, IRequest<TId, TData> request)
        {
            if (request is Async<TId, TData> asyncRequest)
            {
                IEnumerable<IRequest<TId, TData>> requests;

                try
                {
                    requests = await asyncRequest.Requests;
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == state.Token)
                {
                    return;
                }

                await ProcessRequestBatchAsync(state, requests);
                return;
            }

            await RequestStageAsync(state.Handle(request), request);
        }

        private Task SignalStagesAsync(State<TId, TData> state, ISignal<TId, TData> signal)
            => Task.WhenAll(_stages.Select(stage => stage.SignalAsync(signal, state.Token)));

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
            IEnumerable<IRequest<TId, TData>> requests;

            try
            {
                requests = stage.Process(request, state.Token);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == state.Token)
            {
                return Task.CompletedTask;
            }

            return ProcessRequestBatchAsync(state, requests);
        }
        private async Task QuerySourceAsync(State<TId, TData> state, Query<TId, TData> query)
        {
            IReadOnlyDictionary<TId, TData> results;

            try
            {
                results = await _source.ReadAsync(query, state.Token);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == state.Token)
            {
                return;
            }

            var data = new DataSet<TId, TData>(state.Metadata, results);
            await Task.WhenAll(
                SignalStagesAsync(state, new SourceRead<TId, TData>(state.Metadata)),
                RequestStageAsync(state.Handle(data), data));
        }
    }
}
