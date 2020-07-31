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
            .AddParts(typeof(FastSerialHandler))
            .AddParts(typeof(FastParallelHandler))
            .AddParts(typeof(SlowSerialHandler))
            .AddParts(typeof(SlowParallelHandler));

        [Fact]
        public async Task SerialRequestsDontOverlap()
        {
            var requests = new[] {
                new OrderedLspRequest(SlowSerialHandler.MethodName),
                new OrderedLspRequest(SlowSerialHandler.MethodName),
                new OrderedLspRequest(SlowSerialHandler.MethodName),
            };

            await TestAsync(requests);

            // Since every request is serial, no request should have started before the last one ended
            DateTime lastEnd = default;
            foreach (var request in requests)
            {
                Assert.True(request.StartTime > lastEnd);
                lastEnd = request.EndTime;
            }
        }

        [Fact]
        public async Task FastAndSlowSerialRequestsDontOverlap()
        {
            var requests = new[] {
                new OrderedLspRequest(SlowSerialHandler.MethodName),
                new OrderedLspRequest(FastSerialHandler.MethodName),
                new OrderedLspRequest(FastSerialHandler.MethodName),
            };

            await TestAsync(requests);

            // Since every request is serial, no request should have started before the last one ended
            DateTime lastEnd = default;
            foreach (var request in requests)
            {
                Assert.True(request.StartTime > lastEnd);
                lastEnd = request.EndTime;
            }
        }

        private async Task TestAsync(OrderedLspRequest[] requests)
        {
            using var workspace = CreateTestWorkspace("class C { }", out _);
            var solution = workspace.CurrentSolution;

            var languageServer = GetLanguageServer(solution);
            var clientCapabilities = new LSP.ClientCapabilities();

            var waitables = new List<Task<OrderedLspRequest>>();

            var order = 1;
            foreach (var request in requests)
            {
                request.RequestOrder = order++;
                waitables.Add(languageServer.ExecuteRequestAsync<OrderedLspRequest, OrderedLspRequest>(request.MethodName, request, clientCapabilities, null, CancellationToken.None));
            }

            await Task.WhenAll(waitables);

            Assert.Empty(requests.Where(r => r.StartTime == default));

            var byRequestOrder = requests.OrderBy(r => r.RequestOrder).ToArray();
            var byStartTime = requests.OrderBy(r => r.StartTime).ToArray();

            Assert.Equal(byRequestOrder, byStartTime);
        }
    }
}
