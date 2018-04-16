using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sourced.StateMachine;

namespace Sourced
{
    /// <summary>
    /// Represents a series of exceptions which occurred while processing a request in the
    /// <see cref="IPipeline{TId, TData}"/>, along with any results gathered up to that point.
    /// </summary>
    public class PipelineException<TId, TData> : AggregateException
    {
        private static readonly string PipelineMessage = "A series of exceptions occurred within the pipeline.";

        public IReadOnlyDictionary<TId, TData> Results { get; }

        public PipelineException(IReadOnlyDictionary<TId, TData> results, IEnumerable<Exception> innerExceptions) : this(results, innerExceptions.ToArray()) { }
        public PipelineException(IReadOnlyDictionary<TId, TData> results, params Exception[] innerExceptions) : base(PipelineMessage, innerExceptions) { Results = results; }
    }
}
