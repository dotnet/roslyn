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
[ExportRazorLspServiceFactory(typeof(IRazorSemanticTokensRefreshQueue)), Shared]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal class RazorSemanticTokensRefreshQueueWrapperFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var semanticTokensRefreshQueue = lspServices.GetRequiredService<SemanticTokensRefreshQueue>();

        return new RazorSemanticTokensRefreshQueueWrapper(semanticTokensRefreshQueue);
    }

    internal class RazorSemanticTokensRefreshQueueWrapper(SemanticTokensRefreshQueue semanticTokensRefreshQueue) : IRazorSemanticTokensRefreshQueue
    {
        public void Initialize(VSInternalClientCapabilities clientCapabilities)
        {
            // If Roslyn and Razor both support semantic tokens, then this call to Initialize is redundant, but the
            // Initialize method in the queue itself is resilient to being called twice, so it doesn't actually do
            // any harm.
            semanticTokensRefreshQueue.Initialize(clientCapabilities);
            semanticTokensRefreshQueue.AllowRazorRefresh = true;
        }

        public Task TryEnqueueRefreshComputationAsync(Project project, CancellationToken cancellationToken)
            => semanticTokensRefreshQueue.TryEnqueueRefreshComputationAsync(project, cancellationToken);
    }
}
