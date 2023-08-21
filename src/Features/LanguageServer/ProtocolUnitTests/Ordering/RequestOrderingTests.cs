// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    public partial class RequestOrderingTests : AbstractLanguageServerProtocolTests
    {
        public RequestOrderingTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override TestComposition Composition => base.Composition
            .AddParts(typeof(MutatingRequestHandler))
            .AddParts(typeof(NonMutatingRequestHandler))
            .AddParts(typeof(FailingRequestHandler))
            .AddParts(typeof(FailingMutatingRequestHandler))
            .AddParts(typeof(NonLSPSolutionRequestHandler))
            .AddParts(typeof(LongRunningNonMutatingRequestHandler));

        [Theory, CombinatorialData]
        public async Task MutatingRequestsDoNotOverlap(bool mutatingLspWorkspace)
        {
            var requests = new[] {
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
            };

            await using var testLspServer = await CreateTestLspServerAsync("class C { }", mutatingLspWorkspace);
            var responses = await TestAsync(testLspServer, requests);

            // Every request should have started at or after the one before it
            Assert.True(responses[1].StartTime >= responses[0].EndTime);
            Assert.True(responses[2].StartTime >= responses[1].EndTime);
        }

        [Theory, CombinatorialData]
        public async Task NonMutatingRequestsOverlap(bool mutatingLspWorkspace)
        {
            var requests = new[] {
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
            };

            await using var testLspServer = await CreateTestLspServerAsync("class C { }", mutatingLspWorkspace);
            var responses = await TestAsync(testLspServer, requests);

            // Every request should have started immediately, without waiting
            Assert.True(responses[1].StartTime < responses[0].EndTime);
            Assert.True(responses[2].StartTime < responses[1].EndTime);
        }

        [Theory, CombinatorialData]
        public async Task NonMutatingWaitsForMutating(bool mutatingLspWorkspace)
        {
            var requests = new[] {
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
            };

            await using var testLspServer = await CreateTestLspServerAsync("class C { }", mutatingLspWorkspace);
            var responses = await TestAsync(testLspServer, requests);

            // The non mutating tasks should have waited for the first task to finish
            Assert.True(responses[1].StartTime >= responses[0].EndTime);
            Assert.True(responses[2].StartTime >= responses[0].EndTime);
            // The non mutating requests shouldn't have waited for each other
            Assert.True(responses[1].StartTime < responses[2].EndTime);
            Assert.True(responses[2].StartTime < responses[1].EndTime);
        }

        [Theory, CombinatorialData]
        public async Task MutatingDoesntWaitForNonMutating(bool mutatingLspWorkspace)
        {
            var requests = new[] {
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
            };

            await using var testLspServer = await CreateTestLspServerAsync("class C { }", mutatingLspWorkspace);
            var responses = await TestAsync(testLspServer, requests);

            // All tasks should start without waiting for any to finish
            Assert.True(responses[1].StartTime < responses[0].EndTime);
            Assert.True(responses[2].StartTime < responses[0].EndTime);
            Assert.True(responses[1].StartTime < responses[2].EndTime);
            Assert.True(responses[2].StartTime < responses[1].EndTime);
        }

        [Theory, CombinatorialData]
        public async Task ThrowingTaskDoesntBringDownQueue(bool mutatingLspWorkspace)
        {
            var requests = new[] {
                new TestRequest(FailingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
            };

            await using var testLspServer = await CreateTestLspServerAsync("class C { }", mutatingLspWorkspace);
            var waitables = StartTestRun(testLspServer, requests);

            // first task should fail
            await Assert.ThrowsAsync<StreamJsonRpc.RemoteInvocationException>(() => waitables[0]);

            // remaining tasks should have executed normally
            var responses = await Task.WhenAll(waitables.Skip(1));

            Assert.Empty(responses.Where(r => r == null));
            Assert.Empty(responses.Where(r => r!.StartTime == default));
            Assert.All(responses, r => Assert.True(r!.EndTime > r!.StartTime));
        }

        [Theory, CombinatorialData]
        public async Task LongRunningSynchronousNonMutatingTaskDoesNotBlockQueue(bool mutatingLspWorkspace)
        {
            var requests = new[] {
                new TestRequest(LongRunningNonMutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
            };

            await using var testLspServer = await CreateTestLspServerAsync("class C { }", mutatingLspWorkspace);

            // Cancel all requests if the request queue is blocked for 1 minute. This will result in a failed test run.
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var waitables = StartTestRun(testLspServer, requests, cts.Token);

            // Non-long running tasks should run and complete. If there's a test-failure for a "cancellation"
            // at this point it means our long running task blocked the queue and prevented completion.
            var responses = await Task.WhenAll(waitables.Skip(1));
            Assert.Empty(responses.Where(r => r == null));
            Assert.Empty(responses.Where(r => r!.StartTime == default));
            Assert.All(responses, r => Assert.True(r!.EndTime > r!.StartTime));

            // Our long-running waitable should still be running until cancelled.
            var longRunningWaitable = waitables[0];
            Assert.False(longRunningWaitable.IsCompleted);
        }

        [Theory, CombinatorialData]
        public async Task FailingMutableTaskShutsDownQueue(bool mutatingLspWorkspace)
        {
            // NOTE: A failing task shuts down the queue not due to an exception escaping out of the handler
            //       but because the solution state would be invalid. This doesn't test the queues exception
            //       resiliancy.

            var requests = new[] {
                new TestRequest(FailingMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
            };

            await using var testLspServer = await CreateTestLspServerAsync("class C { }", mutatingLspWorkspace);
            var waitables = StartTestRun(testLspServer, requests);

            // first task should fail
            await Assert.ThrowsAsync<StreamJsonRpc.RemoteInvocationException>(() => waitables[0]);

            // The failed request returns to the client before the shutdown completes.
            // Wait for the queue to finish handling the failed request and shutdown.
            await testLspServer.GetQueueAccessor()!.Value.WaitForProcessingToStopAsync().ConfigureAwait(false);

            // remaining tasks should be canceled
            var areAllItemsCancelled = await testLspServer.GetQueueAccessor()!.Value.AreAllItemsCancelledUnsafeAsync();
            Assert.True(areAllItemsCancelled);
        }

        [Theory, CombinatorialData]
        public async Task NonMutatingRequestsOperateOnTheSameSolutionAfterMutation(bool mutatingLspWorkspace)
        {
            await using var testLspServer = await CreateTestLspServerAsync("class C { {|caret:|} }", mutatingLspWorkspace);

            var expectedSolution = testLspServer.GetCurrentSolution();

            // solution should be the same because no mutations have happened
            var solution = await GetLSPSolution(testLspServer, NonMutatingRequestHandler.MethodName);
            Assert.Equal(expectedSolution, solution);

            // Open a document, to get a forked solution
            await ExecuteDidOpen(testLspServer, testLspServer.GetLocations("caret").First().Uri);

            // solution should be different because there has been a mutation
            solution = await GetLSPSolution(testLspServer, NonMutatingRequestHandler.MethodName);
            Assert.NotEqual(expectedSolution, solution);

            expectedSolution = solution;

            // solution should be the same because no mutations have happened
            solution = await GetLSPSolution(testLspServer, NonMutatingRequestHandler.MethodName);
            Assert.Equal(expectedSolution, solution);

            // Apply some random change to the workspace that the LSP server doesn't "see"
            testLspServer.TestWorkspace.SetCurrentSolution(s => s.WithProjectName(s.Projects.First().Id, "NewName"), WorkspaceChangeKind.ProjectChanged);

            expectedSolution = testLspServer.GetCurrentSolution();

            solution = await GetLSPSolution(testLspServer, NonMutatingRequestHandler.MethodName);

            if (mutatingLspWorkspace)
            {
                // In the case where this is a mutating workspace, we would expect the solutions to be the same as
                // everything pushes through between the manager and the workspace, and there have been no text changes
                // in one to cause us to fork.
                Assert.Equal(expectedSolution, solution);
            }
            else
            {
                // in the case where this is a non-mutating solution, the solutions should be different because there
                // has been a workspace change.
                Assert.NotEqual(expectedSolution, solution);
            }

            expectedSolution = solution;

            // solution should be the same because no mutations have happened
            solution = await GetLSPSolution(testLspServer, NonMutatingRequestHandler.MethodName);
            Assert.Equal(expectedSolution, solution);
        }

        [Theory, CombinatorialData]
        public async Task HandlerThatSkipsBuildingLSPSolutionGetsWorkspaceSolution(bool mutatingLspWorkspace)
        {
            await using var testLspServer = await CreateTestLspServerAsync("class C { {|caret:|} }", mutatingLspWorkspace);

            var solution = await GetLSPSolution(testLspServer, NonLSPSolutionRequestHandler.MethodName);
            Assert.Null(solution);

            // Open a document, to create a change that LSP handlers wouldn normally see
            await ExecuteDidOpen(testLspServer, testLspServer.GetLocations("caret").First().Uri);

            // solution shouldn't have changed
            solution = await GetLSPSolution(testLspServer, NonLSPSolutionRequestHandler.MethodName);
            Assert.Null(solution);
        }

        private static async Task ExecuteDidOpen(TestLspServer testLspServer, Uri documentUri)
        {
            var didOpenParams = new LSP.DidOpenTextDocumentParams
            {
                TextDocument = new LSP.TextDocumentItem
                {
                    Uri = documentUri,
                    Text = "// hi there"
                }
            };
            await testLspServer.ExecuteRequestAsync<LSP.DidOpenTextDocumentParams, object>(Methods.TextDocumentDidOpenName, didOpenParams, CancellationToken.None);
        }

        private static async Task<Solution?> GetLSPSolution(TestLspServer testLspServer, string methodName)
        {
            var request = new TestRequest(methodName);
            var response = await testLspServer.ExecuteRequestAsync<TestRequest, TestResponse>(request.MethodName, request, CancellationToken.None);
            Contract.ThrowIfNull(response);
            if (response.ContextHasSolution)
            {
                var (_, solution) = await testLspServer.GetManager().GetLspSolutionInfoAsync(CancellationToken.None).ConfigureAwait(false);
                Contract.ThrowIfNull(solution);
                return solution;
            }

            return null;
        }

        private static async Task<TestResponse[]> TestAsync(TestLspServer testLspServer, TestRequest[] requests)
        {
            var waitables = StartTestRun(testLspServer, requests);

            var responses = await Task.WhenAll(waitables);

            // Sanity checks to ensure test handlers aren't doing something wacky, making future checks invalid
            Assert.Empty(responses.Where(r => r == null));
            Assert.Empty(responses.Where(r => r!.StartTime == default));
            Assert.All(responses, r => Assert.True(r!.EndTime > r!.StartTime));

            return responses!;
        }

        private static List<Task<TestResponse?>> StartTestRun(TestLspServer testLspServer, TestRequest[] requests, CancellationToken cancellationToken = default)
        {
            var waitables = new List<Task<TestResponse?>>();
            foreach (var request in requests)
                waitables.Add(testLspServer.ExecuteRequestAsync<TestRequest, TestResponse>(request.MethodName, request, cancellationToken));

            return waitables;
        }
    }
}
