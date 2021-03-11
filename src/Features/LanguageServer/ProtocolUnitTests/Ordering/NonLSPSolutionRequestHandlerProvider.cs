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
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    [Shared, ExportLspRequestHandlerProvider, PartNotDiscoverable]
    [ProvidesMethod(NonLSPSolutionRequestHandler.MethodName)]
    internal class NonLSPSolutionRequestHandlerProvider : AbstractRequestHandlerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NonLSPSolutionRequestHandlerProvider()
        {
        }

        public override ImmutableArray<IRequestHandler> CreateRequestHandlers()
        {
            return ImmutableArray.Create<IRequestHandler>(new NonLSPSolutionRequestHandler());
        }
    }

    internal class NonLSPSolutionRequestHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public const string MethodName = nameof(NonLSPSolutionRequestHandler);

        public string Method => MethodName;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => false;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequest request) => null;

        public Task<TestResponse> HandleRequestAsync(TestRequest request, RequestContext context, CancellationToken cancellationToken)
        {
            Assert.Null(context.Solution);

            return Task.FromResult(new TestResponse());
        }
    }
}
