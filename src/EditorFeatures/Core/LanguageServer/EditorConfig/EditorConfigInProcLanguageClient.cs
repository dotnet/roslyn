﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.EditorConfig
{
    /// <summary>
    /// Language client to handle .editorconfig LSP requests.
    /// Allows us to move features to LSP without being blocked by TS as well
    /// as ensures that TS LSP features use correct solution snapshots.
    /// </summary>
    [ContentType(ContentTypeNames.EditorConfigContentType)]
    [Export(typeof(ILanguageClient))]
    [Export(typeof(EditorConfigInProcLanguageClient))]
    internal class EditorConfigInProcLanguageClient : AbstractInProcLanguageClient
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public EditorConfigInProcLanguageClient(
            EditorConfigLspServiceProvider lspServiceProvider,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspLoggerFactory lspLoggerFactory,
            IThreadingContext threadingContext)
            : base(lspServiceProvider, globalOptions, listenerProvider, lspLoggerFactory, threadingContext)
        {
        }

        protected override ImmutableArray<string> SupportedLanguages => ImmutableArray.Create(ProtocolConstants.EditorConfigLanguageName);

        public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
        {
            var vsInternalServerCapabilities = new VSInternalServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = TextDocumentSyncKind.Incremental
                },
                HoverProvider = true,
                CompletionProvider = new CompletionOptions
                {
                    ResolveProvider = true,
                    TriggerCharacters = new string[] { " ", "=", "," },
                    AllCommitCharacters = new string[] { "=", " ", "," },
                },
                SupportsDiagnosticRequests = true,
            };

            var serverCapabilities = new ServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = TextDocumentSyncKind.Incremental
                },
                HoverProvider = true,
                CompletionProvider = new CompletionOptions
                {
                    ResolveProvider = true,
                    TriggerCharacters = new string[] { " ", "=", "," },
                    AllCommitCharacters = new string[] { "=", " ", "," },
                },
            };

            var capabilities = clientCapabilities.HasVisualStudioLspCapability() ? vsInternalServerCapabilities : serverCapabilities;

            return capabilities;
        }

        public override bool ShowNotificationOnInitializeFailed => true;

        public override WellKnownLspServerKinds ServerKind => WellKnownLspServerKinds.EditorConfigLspServer;
    }
}
