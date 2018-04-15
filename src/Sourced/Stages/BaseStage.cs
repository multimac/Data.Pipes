using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sourced.Stages
{
    public abstract class BaseStage<TId, TData> : IStage<TId, TData>
    {
        private delegate IEnumerable<IRequest<TId, TData>> RequestHandler(IRequest<TId, TData> request, CancellationToken token);
        protected delegate IEnumerable<IRequest<TId, TData>> RequestHandler<T>(T request, CancellationToken token) where T : IRequest<TId, TData>;

        private Dictionary<Type, RequestHandler> _handlers;

        protected BaseStage() { _handlers = new Dictionary<Type, RequestHandler>(); }

        protected void RegisterRequestHandler<T>(RequestHandler<T> handler) where T : IRequest<TId, TData>
        {
            _handlers.Add(typeof(T), (r, t) => handler((T)r, t));
        }

        public IEnumerable<IRequest<TId, TData>> Process(IRequest<TId, TData> request, CancellationToken token)
        {
            if (!_handlers.TryGetValue(request.GetType(), out var handler))
                return new[] { request };

            return handler(request, token);
        }
    }
}
