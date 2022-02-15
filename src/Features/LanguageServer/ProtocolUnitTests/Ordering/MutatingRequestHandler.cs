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

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    [Shared, ExportRoslynLanguagesLspRequestHandlerProvider(typeof(MutatingRequestHandler)), PartNotDiscoverable]
    [Method(MethodName)]
    internal class MutatingRequestHandler : AbstractStatelessRequestHandler<TestRequest, TestResponse>
    {
        public const string MethodName = nameof(MutatingRequestHandler);
        private const int Delay = 100;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MutatingRequestHandler()
        {
        }

        public override bool MutatesSolutionState => true;
        public override bool RequiresLSPSolution => true;

        public override TextDocumentIdentifier GetTextDocumentIdentifier(TestRequest request) => null;

        public override async Task<TestResponse> HandleRequestAsync(TestRequest request, RequestContext context, CancellationToken cancellationToken)
        {
            var response = new TestResponse
            {
                StartTime = DateTime.UtcNow
            };

            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);

            response.EndTime = DateTime.UtcNow;

            return response;
        }
    }
}
