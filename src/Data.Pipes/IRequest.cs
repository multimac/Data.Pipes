using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Pipes
{
    /// <summary>
    /// Metadata about a <see cref="IRequest{TId, TData}"/>.
    /// </summary>
    public class RequestMetadata
    {
        /// <summary>
        /// An identifier for the current series of requests.
        /// </summary>
        Guid Identifier { get; }

        /// <summary>
        /// Metadata about the pipeline the requests are a part of.
        /// </summary>
        PipelineMetadata Pipeline { get; }

        /// <summary>
        /// Constructs a <see cref="RequestMetadata"/>.
        /// </summary>
        /// <param name="pipelineMetadata">
        /// Metadata about the <see cref="IPipeline{TId, TData}"/> the requests are a part of.
        /// </param>
        internal RequestMetadata(PipelineMetadata pipelineMetadata)
        {
            Identifier = Guid.NewGuid();
            Pipeline = pipelineMetadata;
        }
    }

    /// <summary>
    /// Represents a request to a <see cref="IStage{TId, TData}"/> or <see cref="ISource{TId, TData}"/>
    /// to perform work as part of a <see cref="IPipeline{TId, TData}"/>.
    /// </summary>
    public interface IRequest<TId, TData>
    {
        /// <summary>
        /// Metadata about the request and the pipeline it's a part of.
        /// </summary>
        RequestMetadata Metadata { get; }
    }

    /// <summary>
    /// A specific kind of <see cref="IRequest{TId, TData}"/> to retrieve a series of ids from a 
    /// <see cref="IStage{TId, TData}"/> or <see cref="ISource{TId, TData}"/>.
    /// </summary>
    public interface IQuery<TId, TData> : IRequest<TId, TData>
    {
        /// <summary>
        /// The series of ids to be retrieved.
        /// </summary>
        IReadOnlyCollection<TId> Ids { get; }
    }

    /// <summary>
    /// A specific kind of <see cref="IRequest{TId, TData}"/> which is immediately sent to all
    /// stages in a pipeline to signal some kind of event.
    /// </summary>
    public interface ISignal<TId, TData> : IRequest<TId, TData> { }
}
