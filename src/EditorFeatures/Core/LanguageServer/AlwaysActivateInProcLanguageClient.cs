// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient
{
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
        [ImportMany] IEnumerable<Lazy<ILspBuildOnlyDiagnostics, ILspBuildOnlyDiagnosticsMetadata>> buildOnlyDiagnostics) : AbstractInProcLanguageClient(lspServiceProvider, globalOptions, lspLoggerFactory, threadingContext, exportProvider)
    {
        private readonly ExperimentalCapabilitiesProvider _experimentalCapabilitiesProvider = defaultCapabilitiesProvider;
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

            var isPullDiagnostics = GlobalOptions.IsLspPullDiagnostics();
            if (isPullDiagnostics)
            {
                serverCapabilities.SupportsDiagnosticRequests = true;
                serverCapabilities.MultipleContextSupportProvider = new VSInternalMultipleContextFeatures { SupportsMultipleContextsDiagnostics = true };
                serverCapabilities.DiagnosticProvider ??= new();
                serverCapabilities.DiagnosticProvider.DiagnosticKinds =
                [
                    // Support a specialized requests dedicated to task-list items.  This way the client can ask just
                    // for these, independently of other diagnostics.  They can also throttle themselves to not ask if
                    // the task list would not be visible.
                    new(PullDiagnosticCategories.Task),
                    // Dedicated request for workspace-diagnostics only.  We will only respond to these if FSA is on.
                    new(PullDiagnosticCategories.WorkspaceDocumentsAndProject),
                    // Fine-grained diagnostics requests.  Importantly, this separates out syntactic vs semantic
                    // requests, allowing the former to quickly reach the user without blocking on the latter.  In a
                    // similar vein, compiler diagnostics are explicitly distinct from analyzer-diagnostics, allowing
                    // the former to appear as soon as possible as they are much more critical for the user and should
                    // not be delayed by a slow analyzer.
                    new(PullDiagnosticCategories.DocumentCompilerSyntax),
                    new(PullDiagnosticCategories.DocumentCompilerSemantic),
                    new(PullDiagnosticCategories.DocumentAnalyzerSyntax),
                    new(PullDiagnosticCategories.DocumentAnalyzerSemantic),
                ];
                serverCapabilities.DiagnosticProvider.BuildOnlyDiagnosticIds = _buildOnlyDiagnostics
                    .SelectMany(lazy => lazy.Metadata.BuildOnlyDiagnostics)
                    .Distinct()
                    .ToArray();
            }

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
                        TokenTypes = SemanticTokensSchema.GetSchema(clientCapabilities.HasVisualStudioLspCapability()).AllTokenTypes.ToArray(),
                        TokenModifiers = SemanticTokensSchema.TokenModifiers
                    }
                };
            }

            serverCapabilities.SpellCheckingProvider = true;

            return serverCapabilities;
        }

        /// <summary>
        /// When pull diagnostics is enabled, ensure that initialization failures are displayed to the user as
        /// they will get no diagnostics.  When not enabled we don't show the failure box (failure will still be recorded in the task status center)
        /// as the failure is not catastrophic.
        /// </summary>
        public override bool ShowNotificationOnInitializeFailed => GlobalOptions.IsLspPullDiagnostics();

        public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.AlwaysActiveVSLspServer;
    }
}
