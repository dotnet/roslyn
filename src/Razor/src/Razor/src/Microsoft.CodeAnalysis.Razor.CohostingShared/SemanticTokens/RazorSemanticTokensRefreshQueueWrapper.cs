// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Razor.CohostingShared;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[ExportRazorLspService(typeof(IRazorSemanticTokensRefreshQueue)), Shared(LspServiceComposition.SharingBoundary)]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class RazorSemanticTokensRefreshQueueWrapper(LspService<SemanticTokensRefreshQueue> semanticTokensRefreshQueue) : IRazorSemanticTokensRefreshQueue
{
    public void Initialize(VSInternalClientCapabilities clientCapabilities)
    {
        // If Roslyn and Razor both support semantic tokens, then this call to Initialize is redundant, but the
        // Initialize method in the queue itself is resilient to being called twice, so it doesn't actually do
        // any harm.
        var queue = semanticTokensRefreshQueue.Value;
        queue.Initialize(clientCapabilities);
        queue.AllowRazorRefresh = true;
    }

    public Task TryEnqueueRefreshComputationAsync(Project project, CancellationToken cancellationToken)
        => semanticTokensRefreshQueue.Value.TryEnqueueRefreshComputationAsync(project, cancellationToken);
}
