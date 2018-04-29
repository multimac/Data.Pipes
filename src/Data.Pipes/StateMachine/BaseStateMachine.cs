using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Pipes.StateMachine
{
    /// <summary>
    /// An abstract base class for custom <see cref="IStateMachine{TId, TData}"/> classes to use.
    /// Provides some utility methods to register <see cref="IRequest{TId, TData}"/> handlers.
    /// </summary>
    public abstract class BaseStateMachine<TId, TData> : IStateMachine<TId, TData>
    {
        /// <summary>
        /// Represents a delegate method which can handle the given type of
        /// <see cref="IRequest{TId, TData}"/> received by the state machine.
        /// </summary>
        /// <param name="state">The current state to update.</param>
        /// <param name="request">The request received by the state machine.</param>
        /// <returns>The updated state.</returns>
        protected delegate State<TId, TData> RequestHandler<T>(State<TId, TData> state, T request) where T : IRequest<TId, TData>;
        private delegate State<TId, TData> RequestHandler(State<TId, TData> state, IRequest<TId, TData> request);

        private readonly Dictionary<Type, RequestHandler> _handlers;

        /// <summary>
        /// Constructs a <see cref="BaseStateMachine{TId, TData}"/>.
        /// </summary>
        protected BaseStateMachine()
        {
            _handlers = new Dictionary<Type, RequestHandler>();
        }

        /// <summary>
        /// Registers a method to handle a specific type of <see cref="IRequest{TId, TData}"/>.
        /// </summary>
        /// <param name="handler">
        /// The delegate method to call when the given type of <see cref="IRequest{TId, TData}"/>
        /// is received.
        /// </param>
        /// <typeparam>The type of request the given delegate method will handle.</typeparam>
        protected void RegisterRequestHandler<T>(RequestHandler<T> handler) where T : IRequest<TId, TData>
        {
            _handlers.Add(typeof(T), (s, r) => handler(s, (T)r));
        }

        /// <inheritdoc/>
        public State<TId, TData> Handle(State<TId, TData> state, IRequest<TId, TData> request)
        {
            if (!_handlers.TryGetValue(request.GetType(), out var handler))
                throw new InvalidOperationException($"Invalid type of {nameof(IRequest<TId, TData>)} ({request.GetType()}) given to state machine");

            return handler(state, request);
        }
    }
}
