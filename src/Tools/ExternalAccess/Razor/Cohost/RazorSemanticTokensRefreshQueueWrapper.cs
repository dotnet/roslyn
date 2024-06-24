// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[ExportCohostLspServiceFactory(typeof(IRazorSemanticTokensRefreshQueue)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class RazorSemanticTokensRefreshQueueWrapperFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var semanticTokensRefreshQueue = lspServices.GetRequiredService<SemanticTokensRefreshQueue>();

        return new RazorSemanticTokensRefreshQueueWrapper(semanticTokensRefreshQueue);
    }

    internal class RazorSemanticTokensRefreshQueueWrapper(SemanticTokensRefreshQueue semanticTokensRefreshQueue) : IRazorSemanticTokensRefreshQueue, IDisposable
    {
        public void Initialize(string clientCapabilitiesString)
        {
            var clientCapabilities = JsonSerializer.Deserialize<VSInternalClientCapabilities>(clientCapabilitiesString) ?? new();
            // If Roslyn and Razor both support semantic tokens, then this call to Initialize is redundant, but the
            // Initialize method in the queue itself is resilient to being called twice, so it doesn't actually do
            // any harm.
            semanticTokensRefreshQueue.Initialize(clientCapabilities);
        }

        public Task TryEnqueueRefreshComputationAsync(Project project, CancellationToken cancellationToken)
            => semanticTokensRefreshQueue.TryEnqueueRefreshComputationAsync(project, cancellationToken);

        public void Dispose()
        {
            semanticTokensRefreshQueue.Dispose();
        }
    }
}
