// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;

[ExportCSharpVisualBasicStatelessLspService(typeof(ICapabilitiesProvider), WellKnownLspServerKinds.LiveShareLspServer), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LiveShareCapabilitiesProvider(DefaultCapabilitiesProvider defaultCapabilitiesProvider, IGlobalOptionService globalOptionService) : ICapabilitiesProvider
{
    public ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
    {
        var isLspEditorEnabled = globalOptionService.GetOption(LspOptionsStorage.LspEditorFeatureFlag);

        // If the preview feature flag to turn on the LSP editor in local scenarios is on, advertise no capabilities for this Live Share
        // LSP server as LSP requests will be serviced by the AlwaysActiveInProcLanguageClient in both local and remote scenarios.
        if (isLspEditorEnabled)
        {
            return new VSServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = false,
                    Change = TextDocumentSyncKind.None,
                }
            };
        }

        var defaultCapabilities = defaultCapabilitiesProvider.GetCapabilities(clientCapabilities);

        // If the LSP semantic tokens feature flag is enabled, advertise no semantic tokens capabilities for this Live Share
        // LSP server as LSP semantic tokens requests will be serviced by the AlwaysActiveInProcLanguageClient in both local and
        // remote scenarios.
        var isLspSemanticTokenEnabled = globalOptionService.GetOption(LspOptionsStorage.LspSemanticTokensFeatureFlag);
        if (isLspSemanticTokenEnabled)
        {
            defaultCapabilities.SemanticTokensOptions = null;
        }

        return defaultCapabilities;
    }
}
