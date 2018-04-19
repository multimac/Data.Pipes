using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data.Pipes.StateMachine;

namespace Data.Pipes
{
    /// <summary>
    /// Represents a series of exceptions which occurred while processing a request in the
    /// <see cref="IPipeline{TId, TData}"/>, along with any results gathered up to that point.
    /// </summary>
    public class PipelineException<TId, TData> : AggregateException
    {
        private static readonly string PipelineMessage = "A series of exceptions occurred within the pipeline.";

        /// <summary>
        /// The results which were retrieved for the call to <see cref="IPipeline{TId, TData}.GetAsync(IReadOnlyCollection{TId}, CancellationToken)"/>.
        /// </summary>
        public IReadOnlyDictionary<TId, TData> Results { get; }

        /// <summary>
        /// Constructs a <see cref="PipelineException{TId, TData}"/>
        /// </summary>
        /// <param name="results">The results which were retrieved.</param>
        /// <param name="innerExceptions">The series of exceptions thrown within the pipeline.</param>
        public PipelineException(IReadOnlyDictionary<TId, TData> results, IEnumerable<Exception> innerExceptions)
            : this(results, innerExceptions.ToArray()) { }

        /// <summary>
        /// Constructs a <see cref="PipelineException{TId, TData}"/>
        /// </summary>
        /// <param name="results">The results which were retrieved.</param>
        /// <param name="innerExceptions">The series of exceptions thrown within the pipeline.</param>
        public PipelineException(IReadOnlyDictionary<TId, TData> results, params Exception[] innerExceptions)
            : base(PipelineMessage, innerExceptions) { Results = results; }
    }
}
