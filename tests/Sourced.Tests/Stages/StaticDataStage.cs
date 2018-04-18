using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sourced.Stages;
using Xunit;

namespace Sourced.Tests.Stages
{
    public class StaticDataStage<TId, TData> : BaseStage<TId, TData>, IEnumerable
    {
        private readonly Dictionary<TId, TData> _data;

        public StaticDataStage() : this(new Dictionary<TId, TData>()) { }
        public StaticDataStage(IEnumerable<KeyValuePair<TId, TData>> data)
        {
            _data = data.ToDictionary(pair => pair.Key, pair => pair.Value);
            RegisterRequestHandler<Query<TId, TData>>(Process);
        }

        public void Add(TId key, TData value) => _data.Add(key, value);

        private IEnumerable<IRequest<TId, TData>> Process(Query<TId, TData> query, CancellationToken token)
        {
            var results = query.Ids.Where(_data.ContainsKey).ToDictionary(id => id, id => _data[id]);

            yield return new DataSet<TId, TData>(query.Pipeline, results);
            yield return new Query<TId, TData>(query.Pipeline, query.Ids.Except(results.Keys).ToArray());
        }

        public IEnumerator GetEnumerator() => _data.GetEnumerator();
    }
}
