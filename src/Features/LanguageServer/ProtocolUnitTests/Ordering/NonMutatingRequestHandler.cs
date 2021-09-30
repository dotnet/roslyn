// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System;
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
    [ProvidesMethod(NonMutatingRequestHandler.MethodName)]
    internal class NonMutatingRequestHandlerProvider : AbstractRequestHandlerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NonMutatingRequestHandlerProvider()
        {
        }

        public override ImmutableArray<IRequestHandler> CreateRequestHandlers()
        {
            return ImmutableArray.Create<IRequestHandler>(new NonMutatingRequestHandler());
        }
    }

    internal class NonMutatingRequestHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public const string MethodName = nameof(NonMutatingRequestHandler);
        private const int Delay = 100;

        public string Method => nameof(NonMutatingRequestHandler);

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequest request) => null;

        public async Task<TestResponse> HandleRequestAsync(TestRequest request, RequestContext context, CancellationToken cancellationToken)
        {
            var response = new TestResponse();

            response.Solution = context.Solution;
            response.StartTime = DateTime.UtcNow;

            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);

            // some busy work
            response.ToString();

            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);

            response.EndTime = DateTime.UtcNow;

            return response;
        }
    }
}
