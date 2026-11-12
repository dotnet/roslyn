// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;

internal sealed class HotReloadRequestContext(RequestContext context)
{
    internal LSP.ClientCapabilities ClientCapabilities => context.GetRequiredClientCapabilities();

    [Obsolete("Use GetTextDocumentAsync instead.", error: false)]
    public TextDocument? TextDocument => context.GetTextDocumentAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();

    [Obsolete("Use GetRequiredSolutionAsync instead.", error: false)]
    public Solution? Solution => context.GetRequiredSolutionAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();

    public ValueTask<TextDocument?> GetTextDocumentAsync(CancellationToken cancellationToken)
        => context.GetTextDocumentAsync(cancellationToken);

    [Obsolete("Use GetRequiredSolutionAsync instead.", error: false)]
    public async ValueTask<Solution?> GetSolutionAsync(CancellationToken cancellationToken)
        => await context.GetRequiredSolutionAsync(cancellationToken).ConfigureAwait(false);

    public ValueTask<Solution> GetRequiredSolutionAsync(CancellationToken cancellationToken)
        => context.GetRequiredSolutionAsync(cancellationToken);

    public bool IsTracking(TextDocument textDocument) => context.IsTracking(textDocument.GetURI());
}
