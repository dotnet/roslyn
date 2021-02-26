﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    public partial class RequestOrderingTests : AbstractLanguageServerProtocolTests
    {
        protected override TestComposition Composition => base.Composition
            .AddParts(typeof(MutatingRequestHandlerProvider))
            .AddParts(typeof(NonMutatingRequestHandlerProvider))
            .AddParts(typeof(FailingRequestHandlerProvider))
            .AddParts(typeof(FailingMutatingRequestHandlerProvider))
            .AddParts(typeof(NonLSPSolutionRequestHandlerProvider));

        [Fact]
        public async Task MutatingRequestsDontOverlap()
        {
            var requests = new[] {
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
            };

            using var testLspServer = CreateTestLspServer("class C { }", out _);
            var responses = await TestAsync(testLspServer, requests);

            // Every request should have started at or after the one before it
            Assert.True(responses[1].StartTime >= responses[0].EndTime);
            Assert.True(responses[2].StartTime >= responses[1].EndTime);
        }

        [Fact]
        public async Task NonMutatingRequestsOverlap()
        {
            var requests = new[] {
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
            };

            using var testLspServer = CreateTestLspServer("class C { }", out _);
            var responses = await TestAsync(testLspServer, requests);

            // Every request should have started immediately, without waiting
            Assert.True(responses[1].StartTime < responses[0].EndTime);
            Assert.True(responses[2].StartTime < responses[1].EndTime);
        }

        [Fact]
        public async Task NonMutatingWaitsForMutating()
        {
            var requests = new[] {
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
            };

            using var testLspServer = CreateTestLspServer("class C { }", out _);
            var responses = await TestAsync(testLspServer, requests);

            // The non mutating tasks should have waited for the first task to finish
            Assert.True(responses[1].StartTime >= responses[0].EndTime);
            Assert.True(responses[2].StartTime >= responses[0].EndTime);
            // The non mutating requests shouldn't have waited for each other
            Assert.True(responses[1].StartTime < responses[2].EndTime);
            Assert.True(responses[2].StartTime < responses[1].EndTime);
        }

        [Fact]
        public async Task MutatingDoesntWaitForNonMutating()
        {
            var requests = new[] {
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
            };

            using var testLspServer = CreateTestLspServer("class C { }", out _);
            var responses = await TestAsync(testLspServer, requests);

            // All tasks should start without waiting for any to finish
            Assert.True(responses[1].StartTime < responses[0].EndTime);
            Assert.True(responses[2].StartTime < responses[0].EndTime);
            Assert.True(responses[1].StartTime < responses[2].EndTime);
            Assert.True(responses[2].StartTime < responses[1].EndTime);
        }

        [Fact]
        public async Task ThrowingTaskDoesntBringDownQueue()
        {
            var requests = new[] {
                new TestRequest(FailingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
            };

            using var testLspServer = CreateTestLspServer("class C { }", out _);
            var waitables = StartTestRun(testLspServer, requests);

            // first task should fail
            await Assert.ThrowsAsync<InvalidOperationException>(() => waitables[0]);

            // remaining tasks should have executed normally
            var responses = await Task.WhenAll(waitables.Skip(1));

            Assert.Empty(responses.Where(r => r.StartTime == default));
            Assert.All(responses, r => Assert.True(r.EndTime > r.StartTime));
        }

        [Fact]
        public async Task FailingMutableTaskShutsDownQueue()
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

            using var testLspServer = CreateTestLspServer("class C { }", out _);
            var waitables = StartTestRun(testLspServer, requests);

            // first task should fail
            await Assert.ThrowsAsync<InvalidOperationException>(() => waitables[0]);

            // remaining tasks should be canceled
            await Assert.ThrowsAsync<TaskCanceledException>(() => Task.WhenAll(waitables.Skip(1)));

            Assert.All(waitables.Skip(1), w => Assert.True(w.IsCanceled));
        }

        [Fact]
        public async Task NonMutatingRequestsOperateOnTheSameSolutionAfterMutation()
        {
            using var testLspServer = CreateTestLspServer("class C { {|caret:|} }", out var locations);

            var expectedSolution = testLspServer.GetCurrentSolution();

            // solution should be the same because no mutations have happened
            var solution = await GetLSPSolution(testLspServer, NonMutatingRequestHandler.MethodName);
            Assert.Equal(expectedSolution, solution);

            // Open a document, to get a forked solution
            await ExecuteDidOpen(testLspServer, locations["caret"].First().Uri);

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

            // solution should be different because there has been a workspace change
            solution = await GetLSPSolution(testLspServer, NonMutatingRequestHandler.MethodName);
            Assert.NotEqual(expectedSolution, solution);

            expectedSolution = solution;

            // solution should be the same because no mutations have happened
            solution = await GetLSPSolution(testLspServer, NonMutatingRequestHandler.MethodName);
            Assert.Equal(expectedSolution, solution);
        }

        [Fact]
        public async Task HandlerThatSkipsBuildingLSPSolutionGetsWorkspaceSolution()
        {
            using var testLspServer = CreateTestLspServer("class C { {|caret:|} }", out var locations);

            var solution = await GetLSPSolution(testLspServer, NonLSPSolutionRequestHandler.MethodName);
            Assert.Null(solution);

            // Open a document, to create a change that LSP handlers wouldn normally see
            await ExecuteDidOpen(testLspServer, locations["caret"].First().Uri);

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
            await testLspServer.ExecuteRequestAsync<LSP.DidOpenTextDocumentParams, object>(Methods.TextDocumentDidOpenName, didOpenParams, new LSP.ClientCapabilities(), null, CancellationToken.None);
        }

        private static async Task<Solution> GetLSPSolution(TestLspServer testLspServer, string methodName)
        {
            var request = new TestRequest(methodName);
            var response = await testLspServer.ExecuteRequestAsync<TestRequest, TestResponse>(request.MethodName, request, new LSP.ClientCapabilities(), null, CancellationToken.None);
            return response.Solution;
        }

        private static async Task<TestResponse[]> TestAsync(TestLspServer testLspServer, TestRequest[] requests)
        {
            var waitables = StartTestRun(testLspServer, requests);

            var responses = await Task.WhenAll(waitables);

            // Sanity checks to ensure test handlers aren't doing something wacky, making future checks invalid
            Assert.Empty(responses.Where(r => r.StartTime == default));
            Assert.All(responses, r => Assert.True(r.EndTime > r.StartTime));

            return responses;
        }

        private static List<Task<TestResponse>> StartTestRun(TestLspServer testLspServer, TestRequest[] requests)
        {
            var clientCapabilities = new LSP.ClientCapabilities();

            var waitables = new List<Task<TestResponse>>();
            foreach (var request in requests)
            {
                waitables.Add(testLspServer.ExecuteRequestAsync<TestRequest, TestResponse>(request.MethodName, request, clientCapabilities, null, CancellationToken.None));
            }

            return waitables;
        }
    }
}
