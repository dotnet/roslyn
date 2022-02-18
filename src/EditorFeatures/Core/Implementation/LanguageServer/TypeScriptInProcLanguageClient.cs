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
    /// Language client to handle TS LSP requests.
    /// Allows us to move features to LSP without being blocked by TS as well
    /// as ensures that TS LSP features use correct solution snapshots.
    /// </summary>
    [ContentType(ContentTypeNames.TypeScriptContentTypeName)]
    [ContentType(ContentTypeNames.JavaScriptContentTypeName)]
    [Export(typeof(ILanguageClient))]
    internal class TypeScriptInProcLanguageClient : AbstractInProcLanguageClient
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public TypeScriptInProcLanguageClient(
            RequestDispatcherFactory requestDispatcherFactory,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            DefaultCapabilitiesProvider defaultCapabilitiesProvider,
            ILspLoggerFactory lspLoggerFactory,
            IThreadingContext threadingContext)
            : base(requestDispatcherFactory, globalOptions, listenerProvider, lspWorkspaceRegistrationService, lspLoggerFactory, threadingContext, diagnosticsClientName: null)
        {
        }

        protected override ImmutableArray<string> SupportedLanguages => ImmutableArray.Create(InternalLanguageNames.TypeScript);

        public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
        {
            var serverCapabilities = new VSInternalServerCapabilities();

            serverCapabilities.TextDocumentSync = new TextDocumentSyncOptions
            {
                Change = TextDocumentSyncKind.Incremental,
                OpenClose = true,
            };

            serverCapabilities.ProjectContextProvider = true;

            var isPullDiagnostics = GlobalOptions.IsPullDiagnostics(InternalDiagnosticsOptions.NormalDiagnosticMode);
            if (isPullDiagnostics)
            {
                serverCapabilities.SupportsDiagnosticRequests = true;
                serverCapabilities.MultipleContextSupportProvider = new VSInternalMultipleContextFeatures { SupportsMultipleContextsDiagnostics = true };
            }

            return serverCapabilities;
        }

        /// <summary>
        /// When pull diagnostics is enabled, ensure that initialization failures are displayed to the user as
        /// they will get no diagnostics.  When not enabled we don't show the failure box (failure will still be recorded in the task status center)
        /// as the failure is not catastrophic.
        /// </summary>
        public override bool ShowNotificationOnInitializeFailed => GlobalOptions.IsPullDiagnostics(InternalDiagnosticsOptions.NormalDiagnosticMode);

        public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.RoslynTypeScriptLspServer;
    }
}
