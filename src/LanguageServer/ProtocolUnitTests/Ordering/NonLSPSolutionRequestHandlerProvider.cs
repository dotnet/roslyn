// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering;

[ExportCSharpVisualBasicStatelessLspService(typeof(NonLSPSolutionRequestHandler)), PartNotDiscoverable, Shared]
[Method(MethodName)]
internal sealed class NonLSPSolutionRequestHandler : ILspServiceRequestHandler<TestRequest, TestResponse>
{
    public const string MethodName = nameof(NonLSPSolutionRequestHandler);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public NonLSPSolutionRequestHandler()
    {
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => false;

    public Task<TestResponse> HandleRequestAsync(TestRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        Assert.Null(context.Solution);

        return Task.FromResult(new TestResponse());
    }
}
