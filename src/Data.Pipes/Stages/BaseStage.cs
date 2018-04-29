using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Pipes.Stages
{
    /// <summary>
    /// An abstract base class for custom <see cref="IStage{TId, TData}"/> classes to use. Provides
    /// some utility methods to register <see cref="IRequest{TId, TData}"/> and <see cref="ISignal{TId, TData}"/>
    /// handlers.
    /// </summary>
    public abstract class BaseStage<TId, TData> : IStage<TId, TData>
    {
        /// <summary>
        /// Represents a delegate method which can handle the given type of
        /// <see cref="IRequest{TId, TData}"/> received by the stage.
        /// </summary>
        /// <param name="request">The request received by the stage.</param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to cancel the processing of the
        /// <see cref="IRequest{TId, TData}"/>.
        /// </param>
        /// <returns>A series of further requests to be processed by the pipeline.</returns>
        protected delegate IEnumerable<IRequest<TId, TData>> RequestHandler<T>(T request, CancellationToken token) where T : IRequest<TId, TData>;
        private delegate IEnumerable<IRequest<TId, TData>> RequestHandler(IRequest<TId, TData> request, CancellationToken token);

        /// <summary>
        /// Represents a delegate method which can handle the given type of
        /// <see cref="ISignal{TId, TData}"/> received by the stage.
        /// </summary>
        /// <param name="signal">The signal request received by the stage.</param>
        /// <param name="token">
        /// A <see cref="CancellationToken"/> to cancel the processing of the
        /// <see cref="ISignal{TId, TData}"/>.
        /// </param>
        protected delegate Task SignalHandler<T>(T signal, CancellationToken token) where T : ISignal<TId, TData>;
        private delegate Task SignalHandler(ISignal<TId, TData> signal, CancellationToken token);

        private readonly Dictionary<Type, RequestHandler> _handlers;
        private readonly Dictionary<Type, SignalHandler> _signals;

        /// <summary>
        /// Constructs a <see cref="BaseStage{TId, TData}"/>.
        /// </summary>
        protected BaseStage()
        {
            _handlers = new Dictionary<Type, RequestHandler>();
            _signals = new Dictionary<Type, SignalHandler>();
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
            _handlers.Add(typeof(T), (r, t) => handler((T)r, t));
        }

        /// <summary>
        /// Registers a method to handle a specific type of <see cref="IRequest{TId, TData}"/>.
        /// </summary>
        /// <param name="handler">
        /// The delegate method to call when the given type of <see cref="IRequest{TId, TData}"/>
        /// is received.
        /// </param>
        /// <typeparam>The type of request the given delegate method will handle.</typeparam>
        protected void RegisterSignalHandler<T>(SignalHandler<T> handler) where T : ISignal<TId, TData>
        {
            _signals.Add(typeof(T), (r, t) => handler((T)r, t));
        }

        /// <inheritdoc/>
        public IEnumerable<IRequest<TId, TData>> Process(IRequest<TId, TData> request, CancellationToken token)
        {
            if (!_handlers.TryGetValue(request.GetType(), out var handler))
                return new[] { request };

            return handler(request, token);
        }

        /// <inheritdoc/>
        public Task SignalAsync(ISignal<TId, TData> signal, CancellationToken token)
        {
            if (!_signals.TryGetValue(signal.GetType(), out var handler))
                return Task.CompletedTask;

            return handler(signal, token);
        }
    }
}
