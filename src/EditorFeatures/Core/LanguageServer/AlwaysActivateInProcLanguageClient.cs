// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Utilities;

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
    internal class AlwaysActivateInProcLanguageClient : AbstractInProcLanguageClient
    {
        private readonly DefaultCapabilitiesProvider _defaultCapabilitiesProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public AlwaysActivateInProcLanguageClient(
            RequestDispatcherFactory csharpVBRequestDispatcherFactory,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            DefaultCapabilitiesProvider defaultCapabilitiesProvider,
            ILspLoggerFactory lspLoggerFactory,
            IThreadingContext threadingContext)
            : base(csharpVBRequestDispatcherFactory, globalOptions, listenerProvider, lspWorkspaceRegistrationService, lspLoggerFactory, threadingContext)
        {
            _defaultCapabilitiesProvider = defaultCapabilitiesProvider;
        }

        protected override ImmutableArray<string> SupportedLanguages => ProtocolConstants.RoslynLspLanguages;

        public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
        {
            // If the LSP editor feature flag is enabled advertise support for LSP features here so they are available locally and remote.
            var isLspEditorEnabled = GlobalOptions.GetOption(LspOptions.LspEditorFeatureFlag);

            var serverCapabilities = isLspEditorEnabled
                ? (VSInternalServerCapabilities)_defaultCapabilitiesProvider.GetCapabilities(clientCapabilities)
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

            var isPullDiagnostics = GlobalOptions.IsPullDiagnostics(InternalDiagnosticsOptions.NormalDiagnosticMode);
            if (isPullDiagnostics)
            {
                serverCapabilities.SupportsDiagnosticRequests = true;
                serverCapabilities.MultipleContextSupportProvider = new VSInternalMultipleContextFeatures { SupportsMultipleContextsDiagnostics = true };
            }

            // This capability is always enabled as we provide cntrl+Q VS search only via LSP in ever scenario.
            serverCapabilities.WorkspaceSymbolProvider = true;
            // This capability prevents NavigateTo (cntrl+,) from using LSP symbol search when the server also supports WorkspaceSymbolProvider.
            // Since WorkspaceSymbolProvider=true always to allow cntrl+Q VS search to function, we set DisableGoToWorkspaceSymbols=true
            // when not running the experimental LSP editor.  This ensures NavigateTo uses the existing editor APIs.
            // However, when the experimental LSP editor is enabled we want LSP to power NavigateTo, so we set DisableGoToWorkspaceSymbols=false.
            serverCapabilities.DisableGoToWorkspaceSymbols = !isLspEditorEnabled;

            var isLspSemanticTokensEnabled = GlobalOptions.GetOption(LspOptions.LspSemanticTokensFeatureFlag);
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
                        TokenTypes = SemanticTokenTypes.AllTypes.Concat(SemanticTokensHelpers.RoslynCustomTokenTypes).ToArray(),
                        TokenModifiers = new string[] { SemanticTokenModifiers.Static }
                    }
                };
            }

            return serverCapabilities;
        }

        /// <summary>
        /// When pull diagnostics is enabled, ensure that initialization failures are displayed to the user as
        /// they will get no diagnostics.  When not enabled we don't show the failure box (failure will still be recorded in the task status center)
        /// as the failure is not catastrophic.
        /// </summary>
        public override bool ShowNotificationOnInitializeFailed => GlobalOptions.IsPullDiagnostics(InternalDiagnosticsOptions.NormalDiagnosticMode);

        public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.AlwaysActiveVSLspServer;
    }
}
