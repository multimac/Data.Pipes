using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sourced.Tests.Stages
{
    public class FunctionBasedSource<TId, TData> : ISource<TId, TData>
    {
        private readonly Func<TId, TData> _func;

        public FunctionBasedSource() : this(_ => throw new InvalidOperationException()) { }
        public FunctionBasedSource(Func<TId, TData> func) { _func = func; }

        public Task<IReadOnlyDictionary<TId, TData>> ReadAsync(Query<TId, TData> query, CancellationToken token)
            => Task.FromResult<IReadOnlyDictionary<TId, TData>>(query.Ids.ToDictionary(id => id, id => _func(id)));
    }
}
