using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data.Pipes.Util.Extensions;
using Xunit;

namespace Data.Pipes.Tests.Stages
{
    public class BlockingSource<TId, TData> : ISource<TId, TData>
    {
        private readonly TaskCompletionSource<IReadOnlyDictionary<TId, TData>> _source;

        public BlockingSource() : this(new TaskCompletionSource<IReadOnlyDictionary<TId, TData>>()) { }
        public BlockingSource(TaskCompletionSource<IReadOnlyDictionary<TId, TData>> source) { _source = source; }

        public void SetResult(IReadOnlyDictionary<TId, TData> data)
            => _source.SetResult(data);

        public async Task<IReadOnlyDictionary<TId, TData>> ReadAsync(Query<TId, TData> query, CancellationToken token)
        {
            var tokenTask = Task.Run(async () => await token);
            if (_source.Task == await Task.WhenAny(_source.Task, tokenTask))
                return await _source.Task;

            token.ThrowIfCancellationRequested();
            return null;
        }
    }
}
