using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Pipes
{
    /// <summary>
    /// Represents a request to a <see cref="IStage{TId, TData}"/> or <see cref="ISource{TId, TData}"/>
    /// to perform work as part of a <see cref="IPipeline{TId, TData}"/>.
    /// </summary>
    public interface IRequest<TId, TData>
    {
        /// <summary>
        /// The <see cref="IPipeline{TId, TData}"/> this request is for.
        /// </summary>
        IPipeline<TId, TData> Pipeline { get; }
    }

    /// <summary>
    /// A specific kind of <see cref="IRequest{TId, TData}"/> which is immediately sent to all
    /// stages in a pipeline.
    /// </summary>
    public interface IFlush<TId, TData> : IRequest<TId, TData> { }

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
}
