using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Data.Pipes.Tests.Stages
{
    public class StaticDataSource<TId, TData> : ISource<TId, TData>, IReadOnlyDictionary<TId, TData>
    {
        private readonly Dictionary<TId, TData> _data;

        public int Count => _data.Count;
        public TData this[TId key] => _data[key];

        public IEnumerable<TId> Keys => _data.Keys;
        public IEnumerable<TData> Values => _data.Values;

        public StaticDataSource() : this(new Dictionary<TId, TData>()) { }
        public StaticDataSource(IEnumerable<KeyValuePair<TId, TData>> data)
            => _data = data.ToDictionary(pair => pair.Key, pair => pair.Value);

        public void Add(TId key, TData value) => _data.Add(key, value);

        public Task<IReadOnlyDictionary<TId, TData>> ReadAsync(IQuery<TId, TData> query, CancellationToken token)
            => Task.FromResult<IReadOnlyDictionary<TId, TData>>(query.Ids.Where(_data.ContainsKey).ToDictionary(id => id, id => _data[id]));

        public IEnumerator GetEnumerator() => _data.GetEnumerator();

        public bool ContainsKey(TId key) => _data.ContainsKey(key);
        public bool TryGetValue(TId key, out TData value) => _data.TryGetValue(key, out value);
        IEnumerator<KeyValuePair<TId, TData>> IEnumerable<KeyValuePair<TId, TData>>.GetEnumerator() => _data.GetEnumerator();
    }
}
