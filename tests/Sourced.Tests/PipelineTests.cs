using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Sourced.Tests.Stages;
using Sourced.Util.Extensions;
using Xunit;

namespace Sourced.Tests
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
                .Returns<Query<int, int>, CancellationToken>((r, t) => new[] { new Retry<int, int>(r.Pipeline, r.Ids) });

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
        public async Task OperationCanceledException_Is_Handled_For_Sources_And_No_Results_Are_Returned()
        {
            var completionSource = new TaskCompletionSource<IReadOnlyDictionary<int, int>>();
            var source = new BlockingSource<int, int>(completionSource);

            var pipeline = new Pipeline<int, int>(source);
            var resultsTask = pipeline.GetAsync(new[] { 1, 2 });

            await Assert.ThrowsAsync<TimeoutException>(
                () => resultsTask.TimeoutAfter(TimeSpan.FromMilliseconds(100)));

            completionSource.SetCanceled();

            Assert.Empty(await resultsTask);
        }

        [Fact]
        public async Task OperationCanceledException_Is_Handled_For_Sources_And_Partial_Results_Are_Returned_From_Stages()
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
            source.SetResult(new Dictionary<int, int> { { 1, 2 }, { 3, 4 } });

            Assert.Equal(data, await resultsTask);
        }

        [Fact]
        public async Task OperationCanceledException_Is_Handled_For_Stages()
        {
            async Task<IRequest<int, int>> WaitForCancel(CancellationToken token)
            { await token; token.ThrowIfCancellationRequested(); return null; }

            var source = new FunctionBasedSource<int, int>();
            var stage = new Mock<IStage<int, int>>();

            stage.Setup(s => s.Process(It.IsAny<Query<int, int>>(), It.IsAny<CancellationToken>()))
                .Returns<Query<int, int>, CancellationToken>((r, t) => new[] { new Async<int, int>(r.Pipeline, WaitForCancel(t)) });

            var cancellationSource = new CancellationTokenSource();
            var pipeline = new Pipeline<int, int>(source, stage.Object);
            var resultsTask = pipeline.GetAsync(new[] { 1, 2 }, cancellationSource.Token);

            await Assert.ThrowsAsync<TimeoutException>(
                () => resultsTask.TimeoutAfter(TimeSpan.FromMilliseconds(100)));

            cancellationSource.Cancel();

            Assert.Empty(await resultsTask);
        }
    }
}