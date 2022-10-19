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
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    [Shared, ExportRoslynLanguagesLspRequestHandlerProvider(typeof(LongRunningNonMutatingRequestHandler)), PartNotDiscoverable]
    [Method(MethodName)]
    internal class LongRunningNonMutatingRequestHandler : AbstractStatelessRequestHandler<TestRequest, TestResponse>
    {
        public const string MethodName = nameof(LongRunningNonMutatingRequestHandler);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LongRunningNonMutatingRequestHandler()
        {
        }

        public override bool MutatesSolutionState => false;

        public override bool RequiresLSPSolution => true;

        public override TextDocumentIdentifier GetTextDocumentIdentifier(TestRequest request) => null;

        public override Task<TestResponse> HandleRequestAsync(TestRequest request, RequestContext context, CancellationToken cancellationToken)
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
