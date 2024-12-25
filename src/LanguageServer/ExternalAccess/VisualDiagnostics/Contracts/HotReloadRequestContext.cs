// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;

internal sealed class HotReloadRequestContext(RequestContext context)
{
    internal LSP.ClientCapabilities ClientCapabilities => context.GetRequiredClientCapabilities();
    public TextDocument? TextDocument => context.TextDocument;
    public Solution? Solution => context.Solution;
    public bool IsTracking(TextDocument textDocument) => context.IsTracking(textDocument.GetURI());
}
