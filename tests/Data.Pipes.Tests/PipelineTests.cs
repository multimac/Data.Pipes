using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Data.Pipes.Tests.Stages;
using Data.Pipes.Util.Extensions;
using Xunit;

namespace Data.Pipes.Tests
{
    public class PipelineTests
    {
        [Fact]
        public async Task Can_Retrieve_Data_From_A_Source()
        {
            var data = new Dictionary<int, int> { { 1, 2 }, { 2, 2 }, { 3, 5 } };
            var pipeline = new Pipeline<int, int>(new StaticDataSource<int, int>(data));

            var results = await pipeline.GetAsync(data.Keys);

            Assert.Equal(data, results);
        }

        [Fact]
        public async Task Requests_For_Data_Pass_Through_Configured_Stages()
        {
            var data = new Dictionary<int, int> { { 1, 1 } };
            var source = new StaticDataSource<int, int>(data);
            var stage = new Mock<IStage<int, int>>();

            stage.Setup(s => s.Process(It.IsAny<DataSet<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns<DataSet<int, int>, CancellationToken>((r, t) => new[] { r });
            stage.Setup(s => s.Process(It.IsAny<Query<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns<Query<int, int>, CancellationToken>((r, t) => new[] { r });

            await new Pipeline<int, int>(source, stage.Object).GetAsync(data.Keys);

            stage.Verify(s => s.Process(It.IsAny<Query<int, int>>(), It.IsAny<CancellationToken>()), Times.Once);
            stage.Verify(s => s.Process(It.IsAny<DataSet<int, int>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Retries_Returned_From_First_Stage_Are_Ignored()
        {
            var source = new FunctionBasedSource<int, int>();
            var stage = new Mock<IStage<int, int>>();

            stage.Setup(s => s.Process(It.IsAny<Query<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns<Query<int, int>, CancellationToken>((r, t) => new[] { new Retry<int, int>(r.Metadata, r.Ids) });

            var pipeline = new Pipeline<int, int>(source, stage.Object);
            var results = await pipeline.GetAsync(new[] { 1, 2 });

            Assert.Empty(results);
        }

        [Fact]
        public async Task Results_From_Stages_And_Source_Are_Combined()
        {
            var data = new Dictionary<int, int> { { 1, 4 }, { 2, 3 }, { 5, 9 } };
            var stage = new StaticDataStage<int, int>(data.Where(pair => pair.Key % 2 != 0).ToDictionary(pair => pair.Key, pair => pair.Value));
            var source = new StaticDataSource<int, int>(data.Where(pair => pair.Key % 2 == 0).ToDictionary(pair => pair.Key, pair => pair.Value));

            var pipeline = new Pipeline<int, int>(source, stage);
            var results = await pipeline.GetAsync(data.Keys);

            Assert.Equal(data, results);
        }

        [Fact]
        public async Task SourceRead_And_PipelineComplete_Signals_Are_Sent_To_Stages()
        {
            var source = new StaticDataSource<int, int> { { 1, 1 }, { 2, 2 } };
            var stage = new Mock<IStage<int, int>>();

            stage.Setup(s => s.Process(It.IsAny<IRequest<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns<IRequest<int, int>, CancellationToken>((r, t) => new[] { r });

            var callOrder = 0;
            stage.Setup(s => s.SignalAsync(It.IsAny<SourceRead<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask).Callback(() => Assert.Equal(0, callOrder++));
            stage.Setup(s => s.SignalAsync(It.IsAny<PipelineComplete<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask).Callback(() => Assert.Equal(1, callOrder++));

            await new Pipeline<int, int>(source, stage.Object)
                .GetAsync(source.Keys.ToArray());

            stage.Verify(s => s.SignalAsync(It.IsAny<SourceRead<int, int>>(), It.IsAny<CancellationToken>()), Times.Once);
            stage.Verify(s => s.SignalAsync(It.IsAny<PipelineComplete<int, int>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class PipelineExceptionHandlingTests
    {
        private readonly Mock<IStage<int, int>> _stage;
        private readonly Mock<ISource<int, int>> _source;

        private readonly Pipeline<int, int> _pipeline;

        public PipelineExceptionHandlingTests()
        {
            _stage = new Mock<IStage<int, int>>();
            _stage.Setup(s => s.Process(It.IsAny<IRequest<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns<IRequest<int, int>, CancellationToken>((r, t) => new[] { r });

            _source = new Mock<ISource<int, int>>();
            _source.Setup(s => s.ReadAsync(It.IsAny<Query<int, int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<int, int>());

            _pipeline = new Pipeline<int, int>(_source.Object, _stage.Object);
        }

        public static IEnumerable<object[]> GetPossibleStageExceptions()
        {
            yield return new object[] { (Action<Mock<IStage<int, int>>, Action>)MockStageProcessCall };
            yield return new object[] { (Action<Mock<IStage<int, int>>, Action>)MockStageProcessEnumeration };
            yield return new object[] { (Action<Mock<IStage<int, int>>, Action>)MockAsyncRequest };
        }

        private static void MockStageProcessCall(Mock<IStage<int, int>> stage, Action action)
        {
            IEnumerable<IRequest<int, int>> Process() { action(); return new IRequest<int, int>[] { }; }

            stage.Setup(s => s.Process(It.IsAny<IRequest<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns<IRequest<int, int>, CancellationToken>((r, t) => Process());
        }

        private static void MockStageProcessEnumeration(Mock<IStage<int, int>> stage, Action action)
        {
            IEnumerable<IRequest<int, int>> Process(Query<int, int> request) { yield return new Query<int, int>(request.Metadata, new int[] { }); action(); }

            stage.Setup(s => s.Process(It.IsAny<Query<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns<Query<int, int>, CancellationToken>((r, t) => Process(r));
        }

        private static void MockAsyncRequest(Mock<IStage<int, int>> stage, Action action)
        {
            Task<IEnumerable<IRequest<int, int>>> Process() { action(); return Task.FromResult(Enumerable.Empty<IRequest<int, int>>()); }

            stage.Setup(s => s.Process(It.IsAny<IRequest<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns<IRequest<int, int>, CancellationToken>((r, t) => new[] { new Async<int, int>(r.Metadata, Task.Run(() => Process())) });
        }

        private static void MockSourceRead(Mock<ISource<int, int>> source, Action action)
        {
            Task<IReadOnlyDictionary<int, int>> Process() { action(); return Task.FromResult<IReadOnlyDictionary<int, int>>(new Dictionary<int, int> { }); }

            source.Setup(s => s.ReadAsync(It.IsAny<Query<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run<IReadOnlyDictionary<int, int>>(() => Process()));
        }

        [Theory]
        [MemberData(nameof(GetPossibleStageExceptions))]
        public async Task Token_Matches_And_Thrown_From_Stage_Methods(Action<Mock<IStage<int, int>>, Action> mockSetup)
        {
            var cancellationSource = new CancellationTokenSource();

            mockSetup(_stage, () => { cancellationSource.Cancel(); cancellationSource.Token.ThrowIfCancellationRequested(); });

            var results = await _pipeline.GetAsync(new[] { 1, 2 }, cancellationSource.Token);

            Assert.Empty(results);
        }

        [Theory]
        [MemberData(nameof(GetPossibleStageExceptions))]
        public async Task Token_Is_Different_And_Thrown_From_Stage_Methods(Action<Mock<IStage<int, int>>, Action> mockSetup)
        {
            var cancellationSource = new CancellationTokenSource();

            mockSetup(_stage, () => { cancellationSource.Cancel(); cancellationSource.Token.ThrowIfCancellationRequested(); });

            var exception = await Assert.ThrowsAsync<PipelineException<int, int>>(() => _pipeline.GetAsync(new[] { 1, 2 }));

            Assert.Collection(exception.InnerExceptions,
                ex => Assert.IsAssignableFrom<OperationCanceledException>(ex));
        }

        [Fact]
        public async Task Token_Matches_And_Thrown_From_Source()
        {
            var cancellationSource = new CancellationTokenSource();

            MockSourceRead(_source, () => { cancellationSource.Cancel(); cancellationSource.Token.ThrowIfCancellationRequested(); });

            var results = await _pipeline.GetAsync(new[] { 1, 2 }, cancellationSource.Token);

            Assert.Empty(results);
        }

        [Fact]
        public async Task Token_Is_Different_And_Thrown_From_Source()
        {
            var cancellationSource = new CancellationTokenSource();

            MockSourceRead(_source, () => { cancellationSource.Cancel(); cancellationSource.Token.ThrowIfCancellationRequested(); });

            var exception = await Assert.ThrowsAsync<PipelineException<int, int>>(() => _pipeline.GetAsync(new[] { 1, 2 }));

            Assert.Collection(exception.InnerExceptions,
                ex => Assert.IsAssignableFrom<OperationCanceledException>(ex));
        }

        [Fact]
        public async Task Partial_Results_Are_Returned_When_Exceptions_Occur_In_A_Source()
        {
            var data = new Dictionary<int, int> { { 5, 2 }, { 6, 3 }, { 7, 4 } };
            var source = new BlockingSource<int, int>();
            var stage = new StaticDataStage<int, int>(data);

            var cancellationSource = new CancellationTokenSource();
            var pipeline = new Pipeline<int, int>(source, stage);
            var resultsTask = pipeline.GetAsync(data.Keys, cancellationSource.Token);

            await Assert.ThrowsAsync<TimeoutException>(
                () => resultsTask.TimeoutAfter(TimeSpan.FromMilliseconds(100)));

            cancellationSource.Cancel();

            Assert.Equal(data, await resultsTask);
        }

        [Fact]
        public async Task Exceptions_Are_Handled_When_Thrown_While_Producing_Requests_From_A_Stage()
        {
            var data = new Dictionary<int, int> { { 2, 3 }, { 3, 4 } };
            var keys = data.Keys.Concat(new[] { 1, 4, 5 }).ToArray();

            var completionSource = new TaskCompletionSource<IEnumerable<IRequest<int, int>>>();
            var completionException = new ArgumentException();
            var stageException = new InvalidOperationException();

            var source = new StaticDataSource<int, int>(data);
            var stage = new Mock<IStage<int, int>>();

            IEnumerable<IRequest<int, int>> Process(Query<int, int> request, CancellationToken token)
            {
                yield return new Async<int, int>(request.Metadata, completionSource.Task);
                yield return new Query<int, int>(request.Metadata, request.Ids.Take(request.Ids.Count / 2).ToArray());
                throw stageException;
            }

            completionSource.SetException(completionException);
            stage.Setup(s => s.Process(It.IsAny<Query<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns<Query<int, int>, CancellationToken>(Process);

            var pipeline = new Pipeline<int, int>(source, stage.Object);
            var exception = await Assert.ThrowsAnyAsync<PipelineException<int, int>>(() => pipeline.GetAsync(keys));

            Assert.Equal(data, exception.Results);
            Assert.Equal(2, exception.InnerExceptions.Count);
            Assert.Contains(completionException, exception.InnerExceptions);
            Assert.Contains(stageException, exception.InnerExceptions);
        }
    }
}
