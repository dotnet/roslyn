// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            .AddParts(typeof(NonMutatingRequestHandler));

        [Fact]
        public async Task SerialRequestsDontOverlap()
        {
            var requests = new[] {
                new OrderedLspRequest(MutatingRequestHandler.MethodName),
                new OrderedLspRequest(MutatingRequestHandler.MethodName),
                new OrderedLspRequest(MutatingRequestHandler.MethodName),
            };

            var responses = await TestAsync(requests);

            // Every request should have started at or after the one before it
            Assert.True(responses[1].StartTime >= responses[0].EndTime);
            Assert.True(responses[2].StartTime >= responses[1].EndTime);
        }

        [Fact]
        public async Task ParallelRequestsOverlap()
        {
            var requests = new[] {
                new OrderedLspRequest(NonMutatingRequestHandler.MethodName),
                new OrderedLspRequest(NonMutatingRequestHandler.MethodName),
                new OrderedLspRequest(NonMutatingRequestHandler.MethodName),
            };

            var responses = await TestAsync(requests);

            // Every request should have started immediately, without waiting
            Assert.True(responses[1].StartTime < responses[0].EndTime);
            Assert.True(responses[2].StartTime < responses[1].EndTime);
        }

        [Fact]
        public async Task ParallelWaitsForSerial()
        {
            var requests = new[] {
                new OrderedLspRequest(MutatingRequestHandler.MethodName),
                new OrderedLspRequest(NonMutatingRequestHandler.MethodName),
                new OrderedLspRequest(NonMutatingRequestHandler.MethodName),
            };

            var responses = await TestAsync(requests);

            // The parallel tasks should have waited for the first task to finish
            Assert.True(responses[1].StartTime >= responses[0].EndTime);
            Assert.True(responses[2].StartTime >= responses[0].EndTime);
            // The parallel requests shouldn't have waited for each other
            Assert.True(responses[1].StartTime < responses[2].EndTime);
            Assert.True(responses[2].StartTime < responses[1].EndTime);
        }

        [Fact]
        public async Task ParallelOperatesOnTheRightSolutions()
        {
            var requests = new[] {
                new OrderedLspRequest(NonMutatingRequestHandler.MethodName),
                new OrderedLspRequest(NonMutatingRequestHandler.MethodName),
                new OrderedLspRequest(MutatingRequestHandler.MethodName),
                new OrderedLspRequest(NonMutatingRequestHandler.MethodName),
                new OrderedLspRequest(NonMutatingRequestHandler.MethodName),
            };

            var responses = await TestAsync(requests);

            // first two tasks should have kicked off in parallel
            Assert.True(responses[0].StartTime < responses[1].EndTime);
            Assert.True(responses[1].StartTime < responses[0].EndTime);

            // The last two tasks should have waited for the middle
            Assert.True(responses[3].StartTime >= responses[2].EndTime);
            Assert.True(responses[4].StartTime >= responses[2].EndTime);

            // The last two should have operated on different solutions than the first two
            Assert.NotEqual(responses[3].Solution.WorkspaceVersion, responses[1].Solution.WorkspaceVersion);
            Assert.NotEqual(responses[4].Solution.WorkspaceVersion, responses[1].Solution.WorkspaceVersion);
        }


        private async Task<OrderedLspResponse[]> TestAsync(OrderedLspRequest[] requests)
        {
            using var workspace = CreateTestWorkspace("class C { }", out _);
            var solution = workspace.CurrentSolution;

            var languageServer = GetLanguageServer(solution);
            var clientCapabilities = new LSP.ClientCapabilities();

            var waitables = new List<Task<OrderedLspResponse>>();

            var order = 1;
            foreach (var request in requests)
            {
                request.RequestOrder = order++;
                waitables.Add(languageServer.ExecuteRequestAsync<OrderedLspRequest, OrderedLspResponse>(request.MethodName, request, clientCapabilities, null, CancellationToken.None));
            }

            var responses = await Task.WhenAll(waitables);

            // Sanity checks to ensure test handlers aren't doing something wacky, making future checks invalid
            Assert.Empty(responses.Where(r => r.StartTime == default));
            Assert.All(responses, r => Assert.True(r.EndTime > r.StartTime));

            return responses;
        }
    }
}
