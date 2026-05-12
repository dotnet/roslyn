// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(VSInternalMethods.WorkspaceSpellCheckableRangesName)]
[ExportRazorStatelessLspService(typeof(CohostWorkspaceSpellCheckEndpoint))]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostWorkspaceSpellCheckEndpoint : ILspServiceRequestHandler<VSInternalWorkspaceSpellCheckableParams, VSInternalWorkspaceSpellCheckableReport[]>
{
    bool IMethodHandler.MutatesSolutionState => false;

    bool ISolutionRequiredHandler.RequiresLSPSolution => false;

    // Razor files generally don't do anything at the workspace level

    public Task<VSInternalWorkspaceSpellCheckableReport[]> HandleRequestAsync(VSInternalWorkspaceSpellCheckableParams request, RequestContext context, CancellationToken cancellationToken)
        => SpecializedTasks.EmptyArray<VSInternalWorkspaceSpellCheckableReport>();
}
