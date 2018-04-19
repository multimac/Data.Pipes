using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Pipes.StateMachine
{
    /// <summary>
    /// An interface used when processing <see cref="IRequest{TId, TData}"/> instances through the
    /// pipeline.
    /// </summary>
    public interface IStateMachine<TId, TData>
    {
        /// <summary>
        /// Updates and returns a given state, after applying any changes incurred from the given
        /// <see cref="IRequest{TId, TData}"/>.
        /// </summary>
        /// <param name="state">The state to be updated.</param>
        /// <param name="request">The request triggering the update.</param>
        /// <returns>The updated <see cref="State{TId, TData}"/>.</returns>
        State<TId, TData> Handle(State<TId, TData> state, IRequest<TId, TData> request);
    }
}
