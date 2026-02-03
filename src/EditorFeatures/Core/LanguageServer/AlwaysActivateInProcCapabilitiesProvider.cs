// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;

[ExportCSharpVisualBasicStatelessLspService(typeof(ICapabilitiesProvider), WellKnownLspServerKinds.AlwaysActiveVSLspServer), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class AlwaysActivateInProcCapabilitiesProvider(
    DefaultCapabilitiesProvider defaultCapabilitiesProvider,
    IGlobalOptionService globalOptions,
    IDiagnosticSourceManager diagnosticSourceManager,
    [ImportMany] IEnumerable<Lazy<ILspBuildOnlyDiagnostics, ILspBuildOnlyDiagnosticsMetadata>> buildOnlyDiagnostics) : ICapabilitiesProvider
{
    public ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
    {
        // If the LSP editor feature flag is enabled advertise support for LSP features here so they are available locally and remote.
        var isLspEditorEnabled = globalOptions.GetOption(LspOptionsStorage.LspEditorFeatureFlag);

        var serverCapabilities = isLspEditorEnabled
            ? (VSInternalServerCapabilities)defaultCapabilitiesProvider.GetCapabilities(clientCapabilities)
            : new VSInternalServerCapabilities()
            {
                // Even if the flag is off, we want to include text sync capabilities.
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    Change = TextDocumentSyncKind.Incremental,
                    OpenClose = true,
                },
            };

        serverCapabilities.ProjectContextProvider = true;
        serverCapabilities.BreakableRangeProvider = true;
        serverCapabilities.DataTipRangeProvider = true;

        serverCapabilities.SupportsDiagnosticRequests = true;

        var diagnosticOptions = (serverCapabilities.DiagnosticOptions ??= new DiagnosticOptions());
        diagnosticOptions.Unify().WorkspaceDiagnostics = true;

        serverCapabilities.DiagnosticProvider ??= new();

        // VS does not distinguish between document and workspace diagnostics, so we need to merge them.
        var diagnosticSourceNames = diagnosticSourceManager.GetDocumentSourceProviderNames(clientCapabilities)
            .Concat(diagnosticSourceManager.GetWorkspaceSourceProviderNames(clientCapabilities))
            .Distinct();
        serverCapabilities.DiagnosticProvider = serverCapabilities.DiagnosticProvider with
        {
            SupportsMultipleContextsDiagnostics = true,
            DiagnosticKinds = [.. diagnosticSourceNames.Select(n => new VSInternalDiagnosticKind(n))],
            BuildOnlyDiagnosticIds = [.. buildOnlyDiagnostics
                .SelectMany(lazy => lazy.Metadata.BuildOnlyDiagnostics)
                .Distinct()],
        };

        // This capability is always enabled as we provide cntrl+Q VS search only via LSP in ever scenario.
        serverCapabilities.WorkspaceSymbolProvider = true;
        // This capability prevents NavigateTo (cntrl+,) from using LSP symbol search when the server also supports WorkspaceSymbolProvider.
        // Since WorkspaceSymbolProvider=true always to allow cntrl+Q VS search to function, we set DisableGoToWorkspaceSymbols=true
        // when not running the experimental LSP editor.  This ensures NavigateTo uses the existing editor APIs.
        // However, when the experimental LSP editor is enabled we want LSP to power NavigateTo, so we set DisableGoToWorkspaceSymbols=false.
        serverCapabilities.DisableGoToWorkspaceSymbols = !isLspEditorEnabled;

        var isLspSemanticTokensEnabled = globalOptions.GetOption(LspOptionsStorage.LspSemanticTokensFeatureFlag);
        if (isLspSemanticTokensEnabled)
        {
            // Using only range handling has shown to be more performant than using a combination of full/edits/range handling,
            // especially for larger files. With range handling, we only need to compute tokens for whatever is in view, while
            // with full/edits handling we need to compute tokens for the entire file and then potentially run a diff between
            // the old and new tokens.
            serverCapabilities.SemanticTokensOptions = new SemanticTokensOptions
            {
                Full = false,
                Range = true,
                Legend = new SemanticTokensLegend
                {
                    TokenTypes = [.. SemanticTokensSchema.GetSchema(clientCapabilities.HasVisualStudioLspCapability()).AllTokenTypes],
                    TokenModifiers = SemanticTokensSchema.TokenModifiers
                }
            };
        }

        serverCapabilities.SpellCheckingProvider = true;

        // Enable go to definition, find all references, and go to implementation capabilities for experimentation.
        // These are enabled regardless of the LSP feature flag to allow clients to call these handlers.
        serverCapabilities.DefinitionProvider = true;
        serverCapabilities.ReferencesProvider = new ReferenceOptions
        {
            WorkDoneProgress = true,
        };
        serverCapabilities.ImplementationProvider = true;

        return serverCapabilities;
    }
}
