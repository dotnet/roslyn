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
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    [Shared, ExportRoslynLanguagesLspRequestHandlerProvider, PartNotDiscoverable]
    [ProvidesMethod(LongRunningNonMutatingRequestHandler.MethodName)]
    internal class LongRunningNonMutatingRequestHandlerProvider : AbstractRequestHandlerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LongRunningNonMutatingRequestHandlerProvider()
        {
        }

        public override ImmutableArray<IRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind) => ImmutableArray.Create<IRequestHandler>(new LongRunningNonMutatingRequestHandler());
    }

    internal class LongRunningNonMutatingRequestHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public const string MethodName = nameof(LongRunningNonMutatingRequestHandler);

        public string Method => MethodName;

        public bool MutatesSolutionState => false;

        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequest request) => null;

        public Task<TestResponse> HandleRequestAsync(TestRequest request, RequestContext context, CancellationToken cancellationToken)
        {
            do
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromResult(new TestResponse());
                }

                Thread.Sleep(100);
            } while (true);

            throw new XunitException("Somehow we got past an infinite delay without cancelling. This is unexpected");
        }
    }
}
