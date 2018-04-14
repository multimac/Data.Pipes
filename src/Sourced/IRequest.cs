using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sourced
{
    /// <summary>
    /// Represents a request to a <see cref="IStage{TId, TData}"/> or <see cref="ISource{TId, TData}"/>
    /// to perform work as part of a <see cref="IPipeline"/>.
    /// </summary>
    public interface IRequest<TId, TData>
    {
        /// <summary>
        /// The <see cref="IPipeline{TId, TData}"/> this request is for.
        /// </summary>
        IPipeline<TId, TData> Pipeline { get; }
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
    /// An asynchronous <see cref="IRequest{TId, TData}"/> which will return another type of
    /// request at some point.
    /// </summary>
    /// <remarks>
    /// When this type of <see cref="IRequest{TId, TData}"/> is returned from a <see cref="IStage{TId, TData}"/>,
    /// it indicates to the pipeline that another <see cref="IRequest{TId, TData}"/> will be
    /// returned at some point in the future, and that the pipeline should wait on the contained
    /// <see cref="Task{TResult}"/> for the request. This type of <see cref="IRequest{TId, TData}"/>
    /// will never be passed into a <see cref="IStage{TId, TData}"/>.
    /// </remarks>
    public class Async<TId, TData> : IRequest<TId, TData>
    {
        /// <inheritdoc/>
        public IPipeline<TId, TData> Pipeline { get; }

        /// <summary>
        /// A <see cref="Task{TResult}"/> returning the actual request at some point in the future.
        /// </summary>
        public Task<IRequest<TId, TData>> Request { get; }

        /// <summary>
        /// Constructs a <see cref="Async{TId, TData}"/>.
        /// </summary>
        /// <param name="pipeline">The <see cref="IPipeline{TId, TData}"/> this request is for.</param>
        /// <param name="request">The <see cref="Task{TResult}"/> returning the actual request.</param>
        public Async(IPipeline<TId, TData> pipeline, Task<IRequest<TId, TData>> request)
        {
            Pipeline = pipeline;
            Request = request;
        }
    }

    /// <summary>
    /// A <see cref="IRequest{TId, TData}"/> containing a dictionary of ids and their corresponding
    /// data objects.
    /// </summary>
    /// <remarks>
    /// When this type of <see cref="IRequest{TId, TData}"/> is returned from a <see cref="IStage{TId, TData}"/>,
    /// the pipeline will add it to the collection of results for a request and pass it to the
    /// previous stage to be cached. When a stage receives this type of <see cref="IRequest{TId, TData}"/>,
    /// it should attempt to cache the contained results, or return it as-is to be passed further
    /// along the pipeline.
    /// </remarks>
    public class DataSet<TId, TData> : IRequest<TId, TData>
    {
        /// <inheritdoc/>
        public IPipeline<TId, TData> Pipeline { get; }

        /// <summary>
        /// The dictionary of ids and their corresponding data objects.
        /// </summary>
        public IReadOnlyDictionary<TId, TData> Results { get; }

        /// <summary>
        /// Constructs a <see cref="DataSet{TId, TData}"/>.
        /// </summary>
        /// <param name="pipeline">The <see cref="IPipeline{TId, TData}"/> this request is for.</param>
        /// <param name="results">The results contained in this <see cref="DataSet{TId, TData}"/>.</param>
        public DataSet(IPipeline<TId, TData> pipeline, IReadOnlyDictionary<TId, TData> results)
        {
            Pipeline = pipeline;
            Results = results;
        }
    }

    /// <summary>
    /// A query to retrieve a given series of ids.
    /// </summary>
    /// <remarks>
    /// When this type of <see cref="IRequest{TId, TData}"/> is returned from a <see cref="IStage{TId, TData}"/>,
    /// the pipeline will pass it to the next stage. When a stage receives this type of <see cref="IRequest{TId, TData}"/>,
    /// it should attempt to retrieve as many of the contained ids as possible, and any remaining
    /// ids should be returned in another <see cref="Query{TId, TData}"/>.
    /// </remarks>
    public class Query<TId, TData> : IQuery<TId, TData>
    {
        /// <inheritdoc/>
        public IPipeline<TId, TData> Pipeline { get; }

        /// <inheritdoc/>
        public IReadOnlyCollection<TId> Ids { get; }

        /// <summary>
        /// Constructs a <see cref="Query{TId, TData}"/>.
        /// </summary>
        /// <param name="pipeline">The <see cref="IPipeline{TId, TData}"/> this request is for.</param>
        /// <param name="results">The series of ids to be retrieved.</param>
        public Query(IPipeline<TId, TData> pipeline, IReadOnlyCollection<TId> ids)
        {
            Pipeline = pipeline;
            Ids = ids;
        }
    }

    /// <summary>
    /// A request to try and retrieve the given series of ids again.
    /// </summary>
    /// <remarks>
    /// This type of <see cref="IRequest{TId, TData}"/> is similar to a <see cref="Query{TId, TData}"/>,
    /// however instead of the pipeline passing it to the next stage, it will pass it to the
    /// previous stage. Stages should treat this the the same as a <see cref="Query{TId, TData}"/>,
    /// however, if only partial results can be returned, a <see cref="Retry{TId, TData}"/> with
    /// the remaining ids should not be returned.
    /// </remarks>
    public class Retry<TId, TData> : IQuery<TId, TData>
    {
        /// <inheritdoc/>
        public IPipeline<TId, TData> Pipeline { get; }

        /// <inheritdoc/>
        public IReadOnlyCollection<TId> Ids { get; }

        /// <summary>
        /// Constructs a <see cref="Retry{TId, TData}"/>.
        /// </summary>
        /// <param name="pipeline">The <see cref="IPipeline{TId, TData}"/> this request is for.</param>
        /// <param name="results">The series of ids to be retrieved.</param>
        public Retry(IPipeline<TId, TData> pipeline, IReadOnlyCollection<TId> ids)
        {
            Pipeline = pipeline;
            Ids = ids;
        }
    }
}
