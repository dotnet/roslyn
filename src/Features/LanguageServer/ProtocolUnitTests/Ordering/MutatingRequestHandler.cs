// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    [Shared, ExportLspRequestHandlerProvider, PartNotDiscoverable]
    internal class MutatingRequestHandlerProvider : AbstractRequestHandlerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MutatingRequestHandlerProvider()
        {
        }

        protected override IEnumerable<IRequestHandler> InitializeHandlers()
        {
            return ImmutableArray.Create(new MutatingRequestHandler());
        }
    }

    [LspMethod(MethodName, mutatesSolutionState: true)]
    internal class MutatingRequestHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public const string MethodName = nameof(MutatingRequestHandler);
        private const int Delay = 100;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequest request) => null;

        public async Task<TestResponse> HandleRequestAsync(TestRequest request, RequestContext context, CancellationToken cancellationToken)
        {
            var response = new TestResponse
            {
                Solution = context.Solution,
                StartTime = DateTime.UtcNow
            };

            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);

            response.EndTime = DateTime.UtcNow;

            return response;
        }
    }
}
