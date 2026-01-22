// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

/// <summary>
/// Language client to handle TS LSP requests.
/// Allows us to move features to LSP without being blocked by TS as well
/// as ensures that TS LSP features use correct solution snapshots.
/// </summary>
[ExportLspServiceFactory(typeof(ICapabilitiesProvider), ProtocolConstants.TypeScriptLanguageContract, WellKnownLspServerKinds.RoslynTypeScriptLspServer)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
[Shared]
internal class VSTypeScriptInProcLanguageClientCapabilitiesProvider() : ICapabilitiesProvider
{
    public ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
    {
        var serverCapabilities = new VSInternalServerCapabilities
        {
            TextDocumentSync = new TextDocumentSyncOptions
            {
                Change = TextDocumentSyncKind.Incremental,
                OpenClose = true,
            },

            ProjectContextProvider = true,

            SupportsDiagnosticRequests = true,
            DiagnosticProvider = new()
            {
                SupportsMultipleContextsDiagnostics = true,
                DiagnosticKinds =
                [
                    new(PullDiagnosticCategories.Task),
                    new(PullDiagnosticCategories.WorkspaceDocumentsAndProject),
                    new(PullDiagnosticCategories.DocumentAnalyzerSyntax),
                    new(PullDiagnosticCategories.DocumentAnalyzerSemantic),
                ]
            }
        };

        return serverCapabilities;
    }
}
