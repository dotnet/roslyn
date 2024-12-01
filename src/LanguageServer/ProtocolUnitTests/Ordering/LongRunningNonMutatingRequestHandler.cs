// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(LongRunningNonMutatingRequestHandler)), PartNotDiscoverable, Shared]
    [Method(MethodName)]
    internal class LongRunningNonMutatingRequestHandler : ILspServiceRequestHandler<TestRequest, TestResponse>
    {
        public const string MethodName = nameof(LongRunningNonMutatingRequestHandler);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LongRunningNonMutatingRequestHandler()
        {
        }

        public bool MutatesSolutionState => false;

        public bool RequiresLSPSolution => true;

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
