// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    [Shared, ExportRoslynLanguagesLspRequestHandlerProvider(typeof(NonLSPSolutionRequestHandler)), PartNotDiscoverable]
    [Method(MethodName)]
    internal class NonLSPSolutionRequestHandler : AbstractStatelessRequestHandler<TestRequest, TestResponse>
    {
        public const string MethodName = nameof(NonLSPSolutionRequestHandler);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NonLSPSolutionRequestHandler()
        {
        }

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => false;

        public override TextDocumentIdentifier GetTextDocumentIdentifier(TestRequest request) => null;

        public override Task<TestResponse> HandleRequestAsync(TestRequest request, RequestContext context, CancellationToken cancellationToken)
        {
            Assert.Null(context.Solution);

            return Task.FromResult(new TestResponse());
        }
    }
}
