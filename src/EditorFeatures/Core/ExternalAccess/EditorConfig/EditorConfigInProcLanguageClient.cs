// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig
{
    /// <summary>
    /// Language client to handle TS LSP requests.
    /// Allows us to move features to LSP without being blocked by TS as well
    /// as ensures that TS LSP features use correct solution snapshots.
    /// </summary>
    [ContentType(ContentTypeNames.EditorConfigContentType)]
    [Export(typeof(ILanguageClient))]
    [Export(typeof(EditorConfigInProcLanguageClient))]
    internal class EditorConfigInProcLanguageClient : AbstractInProcLanguageClient
    {
        private readonly IEditorConfigCapabilitiesProvider? _editorConfigCapabilitiesProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public EditorConfigInProcLanguageClient(
            [Import(AllowDefault = true)] IEditorConfigCapabilitiesProvider? editorConfigCapabilitiesProvider,
            EditorConfigLspServiceProvider lspServiceProvider,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspLoggerFactory lspLoggerFactory,
            IThreadingContext threadingContext)
            : base(lspServiceProvider, globalOptions, listenerProvider, lspLoggerFactory, threadingContext)
        {
            _editorConfigCapabilitiesProvider = editorConfigCapabilitiesProvider;
        }

        protected override ImmutableArray<string> SupportedLanguages => ImmutableArray.Create(StringConstants.EditorConfigLanguageName);

        public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
        {
            var serverCapabilities = GetEditorConfigServerCapabilities(clientCapabilities);

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
            }

            return serverCapabilities;
        }

        /// <summary>
        /// When pull diagnostics is enabled, ensure that initialization failures are displayed to the user as
        /// they will get no diagnostics.  When not enabled we don't show the failure box (failure will still be recorded in the task status center)
        /// as the failure is not catastrophic.
        /// </summary>
        public override bool ShowNotificationOnInitializeFailed => GlobalOptions.IsPullDiagnostics(InternalDiagnosticsOptions.NormalDiagnosticMode);

        public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.EditorConfigLspServer;

        private VSInternalServerCapabilities GetEditorConfigServerCapabilities(ClientCapabilities clientCapabilities)
        {
            if (_editorConfigCapabilitiesProvider != null)
            {
                var serializedClientCapabilities = JsonConvert.SerializeObject(clientCapabilities);
                var serializedServerCapabilities = _editorConfigCapabilitiesProvider.GetServerCapabilities(serializedClientCapabilities);
                var editorConfigServerCapabilities = JsonConvert.DeserializeObject<VSInternalServerCapabilities>(serializedServerCapabilities);
                Contract.ThrowIfNull(editorConfigServerCapabilities);
                return editorConfigServerCapabilities;
            }
            else
            {
                return new VSInternalServerCapabilities();
            }
        }
    }
}
