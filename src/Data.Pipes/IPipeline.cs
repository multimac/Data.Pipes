using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Pipes
{
    /// <summary>
    /// Metadata about a <see cref="IPipeline{TId, TData}"/>.
    /// </summary>
    public class PipelineMetadata
    {
        /// <summary>
        /// The type of the source in the pipeline.
        /// </summary>
        public Type SourceType { get; }

        /// <summary>
        /// Constructs a <see cref="PipelineMetadata"/>.
        /// </summary>
        /// <param name="sourceType">The type of the source in the pipeline.</param>
        private PipelineMetadata(Type sourceType) { SourceType = sourceType; }

        /// <summary>
        /// Creates a <see cref="PipelineMetadata"/>.
        /// </summary>
        /// <param name="source">The source in the pipeline.</param>
        /// <returns>An instance of <see cref="PipelineMetadata"/>.</returns>
        internal static PipelineMetadata CreateFromSource<TId, TData>(ISource<TId, TData> source)
            => new PipelineMetadata(source.GetType());
    }

    /// <summary>
    /// Represents a pipeline of <see cref="IStage{TId, TData}"/> instances and a final
    /// <see cref="ISource{TId, TData}"/> where data is retrieved from.
    /// </summary>
    /// <typeparam name="TId">
    /// The type of the id objects used in the final <see cref="ISource{TId, TData}"/>.
    /// </typeparam>
    /// <typeparam name="TData">
    /// The type of the data objects used in the final <see cref="ISource{TId, TData}"/>.
    /// </typeparam>
    public interface IPipeline<TId, TData>
    {
        /// <summary>
        /// Metadata about the pipeline itself.
        /// </summary>
        PipelineMetadata Metadata { get; }

        /// <summary>
        /// Retrieves data for as many of the given objects as possible.
        /// </summary>
        /// <param name="ids">
        /// The series of ids to retrieve data for.
        /// </param>
        /// <param name="token">
        /// A cancellation token used to cancel the request and return the data retrieved so far.
        /// </param>
        /// <returns>
        /// A dictionary containing all the data that was able to be retrieved, and their ids.
        /// </returns>
        Task<IReadOnlyDictionary<TId, TData>> GetAsync(IReadOnlyCollection<TId> ids, CancellationToken token = default);
    }
}
