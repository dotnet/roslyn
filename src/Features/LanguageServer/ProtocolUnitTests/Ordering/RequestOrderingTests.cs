// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    public partial class RequestOrderingTests : AbstractLanguageServerProtocolTests
    {
        protected override TestComposition Composition => base.Composition
            .AddParts(typeof(MutatingRequestHandler))
            .AddParts(typeof(NonMutatingRequestHandler))
            .AddParts(typeof(FailingRequestHandler))
            .AddParts(typeof(FailingMutatingRequestHandler));

        [Fact]
        public async Task MutatingRequestsDontOverlap()
        {
            var requests = new[] {
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
            };

            var responses = await TestAsync(requests);

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

            var responses = await TestAsync(requests);

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

            var responses = await TestAsync(requests);

            // The non mutating tasks should have waited for the first task to finish
            Assert.True(responses[1].StartTime >= responses[0].EndTime);
            Assert.True(responses[2].StartTime >= responses[0].EndTime);
            // The non mutating requests shouldn't have waited for each other
            Assert.True(responses[1].StartTime < responses[2].EndTime);
            Assert.True(responses[2].StartTime < responses[1].EndTime);
        }

        [Fact]
        public async Task NonMutatingOperatesOnTheRightSolutions()
        {
            var requests = new[] {
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
            };

            var responses = await TestAsync(requests);

            // first two tasks should have kicked off without waiting
            Assert.True(responses[0].StartTime < responses[1].EndTime);
            Assert.True(responses[1].StartTime < responses[0].EndTime);

            // The mutating task should have kicked off without waiting for those to finish
            Assert.True(responses[2].StartTime < responses[1].EndTime);
            Assert.True(responses[2].StartTime < responses[0].EndTime);

            // The last two tasks should have waited for the mutating task
            Assert.True(responses[3].StartTime >= responses[2].EndTime);
            Assert.True(responses[4].StartTime >= responses[2].EndTime);

            // The last two should have operated on different solutions than the first three
            Assert.NotEqual(responses[3].Solution.WorkspaceVersion, responses[0].Solution.WorkspaceVersion);
            Assert.NotEqual(responses[3].Solution.WorkspaceVersion, responses[1].Solution.WorkspaceVersion);
            Assert.NotEqual(responses[3].Solution.WorkspaceVersion, responses[2].Solution.WorkspaceVersion);
            Assert.Equal(responses[3].Solution.WorkspaceVersion, responses[3].Solution.WorkspaceVersion);
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

            var waitables = StartTestRun(requests);

            // first task should fail
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await waitables[0]);

            // remaining tasks should have executed normally
            var responses = await Task.WhenAll(waitables.Skip(1));

            Assert.Empty(responses.Where(r => r.StartTime == default));
            Assert.All(responses, r => Assert.True(r.EndTime > r.StartTime));
        }

        [Fact]
        public async Task ThrowingMutableTaskDoesntBringDownQueue()
        {
            var requests = new[] {
                new TestRequest(FailingMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(MutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
            };

            var waitables = StartTestRun(requests);

            // first task should fail
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await waitables[0]);

            // remaining tasks should have executed normally
            var responses = await Task.WhenAll(waitables.Skip(1));

            Assert.Empty(responses.Where(r => r.StartTime == default));
            Assert.All(responses, r => Assert.True(r.EndTime > r.StartTime));
        }

        [Fact]
        public async Task ThrowingMutableTaskDoesntMutateTheSolution()
        {
            var requests = new[] {
                new TestRequest(NonMutatingRequestHandler.MethodName),
                new TestRequest(FailingMutatingRequestHandler.MethodName),
                new TestRequest(NonMutatingRequestHandler.MethodName),
            };

            var waitables = StartTestRun(requests);

            // second task should have failed
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await waitables[1]);

            var responses = await Task.WhenAll(waitables.Where(t => !t.IsFaulted));

            // First and last tasks use the same solution because the middle request failed
            Assert.Equal(responses[0].Solution.WorkspaceVersion, responses[2].Solution.WorkspaceVersion);
        }

        private async Task<TestResponse[]> TestAsync(TestRequest[] requests)
        {
            var waitables = StartTestRun(requests);

            var responses = await Task.WhenAll(waitables);

            // Sanity checks to ensure test handlers aren't doing something wacky, making future checks invalid
            Assert.Empty(responses.Where(r => r.StartTime == default));
            Assert.All(responses, r => Assert.True(r.EndTime > r.StartTime));

            return responses;
        }

        private List<Task<TestResponse>> StartTestRun(TestRequest[] requests)
        {
            using var workspace = CreateTestWorkspace("class C { }", out _);
            var solution = workspace.CurrentSolution;

            var languageServer = GetLanguageServer(solution);
            var clientCapabilities = new LSP.ClientCapabilities();

            var waitables = new List<Task<TestResponse>>();
            var order = 1;
            foreach (var request in requests)
            {
                request.RequestOrder = order++;
                waitables.Add(languageServer.ExecuteRequestAsync<TestRequest, TestResponse>(request.MethodName, request, clientCapabilities, null, CancellationToken.None));
            }

            return waitables;
        }
    }
}
