using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sourced.Tests.Stages
{
    public class StaticDataSource<TId, TData> : ISource<TId, TData>, IEnumerable
    {
        private readonly Dictionary<TId, TData> _data;

        public StaticDataSource() : this(new Dictionary<TId, TData>()) { }
        public StaticDataSource(IEnumerable<KeyValuePair<TId, TData>> data)
            => _data = data.ToDictionary(pair => pair.Key, pair => pair.Value);

        public void Add(TId key, TData value) => _data.Add(key, value);

        public Task<IReadOnlyDictionary<TId, TData>> ReadAsync(Query<TId, TData> query, CancellationToken token)
            => Task.FromResult<IReadOnlyDictionary<TId, TData>>(query.Ids.Where(_data.ContainsKey).ToDictionary(id => id, id => _data[id]));

        public IEnumerator GetEnumerator() => _data.GetEnumerator();
    }
}
