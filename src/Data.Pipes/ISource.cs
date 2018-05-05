using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Pipes
{
    /// <summary>
    /// A source of data for a <see cref="IPipeline{TId, TData}"/>.
    /// </summary>
    /// <typeparam name="TId">
    /// The type of the id objects this source retrieves data for.
    /// </typeparam>
    /// <typeparam name="TData">
    /// The type of the data objects retrieved by this source.
    /// </typeparam>
    public interface ISource<TId, TData>
    {
        /// <summary>
        /// Queries the source for a given series of ids.
        /// </summary>
        /// <param name="query">The query containing the ids to retrieve.</param>
        /// <param name="token">
        /// A cancellation token used to cancel the query. Partial results may be returned if
        /// available.
        /// </param>
        /// <returns>
        /// A <see cref="IReadOnlyDictionary{TId, TData}"/> containing data for all the ids which were able to
        /// be retrieved.
        /// </returns>
        Task<IReadOnlyDictionary<TId, TData>> ReadAsync(IQuery<TId, TData> query, CancellationToken token);
    }
}
