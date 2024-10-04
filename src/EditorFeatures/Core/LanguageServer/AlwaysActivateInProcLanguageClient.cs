// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;

/// <summary>
/// Language client responsible for handling C# / VB / F# LSP requests in any scenario (both local and codespaces).
/// This powers "LSP only" features (e.g. cntrl+Q code search) that do not use traditional editor APIs.
/// It is always activated whenever roslyn is activated.
/// </summary>
[ContentType(ContentTypeNames.CSharpContentType)]
[ContentType(ContentTypeNames.VisualBasicContentType)]
[ContentType(ContentTypeNames.FSharpContentType)]
[Export(typeof(ILanguageClient))]
[Export(typeof(AlwaysActivateInProcLanguageClient))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, true)]
internal class AlwaysActivateInProcLanguageClient(
    CSharpVisualBasicLspServiceProvider lspServiceProvider,
    IGlobalOptionService globalOptions,
    ExperimentalCapabilitiesProvider defaultCapabilitiesProvider,
    ILspServiceLoggerFactory lspLoggerFactory,
    IThreadingContext threadingContext,
    ExportProvider exportProvider,
    IDiagnosticSourceManager diagnosticSourceManager,
    [ImportMany] IEnumerable<Lazy<ILspBuildOnlyDiagnostics, ILspBuildOnlyDiagnosticsMetadata>> buildOnlyDiagnostics) : AbstractInProcLanguageClient(lspServiceProvider, globalOptions, lspLoggerFactory, threadingContext, exportProvider)
{
    private readonly ExperimentalCapabilitiesProvider _experimentalCapabilitiesProvider = defaultCapabilitiesProvider;
    private readonly IDiagnosticSourceManager _diagnosticSourceManager = diagnosticSourceManager;
    private readonly IEnumerable<Lazy<ILspBuildOnlyDiagnostics, ILspBuildOnlyDiagnosticsMetadata>> _buildOnlyDiagnostics = buildOnlyDiagnostics;

    protected override ImmutableArray<string> SupportedLanguages => ProtocolConstants.RoslynLspLanguages;

    public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
    {
        // If the LSP editor feature flag is enabled advertise support for LSP features here so they are available locally and remote.
        var isLspEditorEnabled = GlobalOptions.GetOption(LspOptionsStorage.LspEditorFeatureFlag);

        var serverCapabilities = isLspEditorEnabled
            ? (VSInternalServerCapabilities)_experimentalCapabilitiesProvider.GetCapabilities(clientCapabilities)
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

        serverCapabilities.SupportsDiagnosticRequests = true;

        var diagnosticOptions = (serverCapabilities.DiagnosticOptions ??= new DiagnosticOptions());
        diagnosticOptions.Unify().WorkspaceDiagnostics = true;

        serverCapabilities.DiagnosticProvider ??= new();

        // VS does not distinguish between document and workspace diagnostics, so we need to merge them.
        var diagnosticSourceNames = _diagnosticSourceManager.GetDocumentSourceProviderNames(clientCapabilities)
            .Concat(_diagnosticSourceManager.GetWorkspaceSourceProviderNames(clientCapabilities))
            .Distinct();
        serverCapabilities.DiagnosticProvider = serverCapabilities.DiagnosticProvider with
        {
            SupportsMultipleContextsDiagnostics = true,
            DiagnosticKinds = diagnosticSourceNames.Select(n => new VSInternalDiagnosticKind(n)).ToArray(),
            BuildOnlyDiagnosticIds = _buildOnlyDiagnostics
                .SelectMany(lazy => lazy.Metadata.BuildOnlyDiagnostics)
                .Distinct()
                .ToArray(),
        };

        // This capability is always enabled as we provide cntrl+Q VS search only via LSP in ever scenario.
        serverCapabilities.WorkspaceSymbolProvider = true;
        // This capability prevents NavigateTo (cntrl+,) from using LSP symbol search when the server also supports WorkspaceSymbolProvider.
        // Since WorkspaceSymbolProvider=true always to allow cntrl+Q VS search to function, we set DisableGoToWorkspaceSymbols=true
        // when not running the experimental LSP editor.  This ensures NavigateTo uses the existing editor APIs.
        // However, when the experimental LSP editor is enabled we want LSP to power NavigateTo, so we set DisableGoToWorkspaceSymbols=false.
        serverCapabilities.DisableGoToWorkspaceSymbols = !isLspEditorEnabled;

        var isLspSemanticTokensEnabled = GlobalOptions.GetOption(LspOptionsStorage.LspSemanticTokensFeatureFlag);
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

        return serverCapabilities;
    }

    public override bool ShowNotificationOnInitializeFailed => true;

    public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.AlwaysActiveVSLspServer;
}
