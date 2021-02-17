// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    [DisableUserExperience(true)] // Remove this when we are ready to use LSP everywhere
    [ContentType(ContentTypeNames.XamlContentType)]
    [Export(typeof(ILanguageClient))]
    internal class XamlInProcLanguageClient : AbstractInProcLanguageClient
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public XamlInProcLanguageClient(
            XamlLanguageServerProtocol languageServerProtocol,
            VisualStudioWorkspace workspace,
            IDiagnosticService diagnosticService,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            [Import(typeof(SAsyncServiceProvider))] VSShell.IAsyncServiceProvider asyncServiceProvider,
            IThreadingContext threadingContext)
            : base(languageServerProtocol, workspace, diagnosticService, listenerProvider, lspWorkspaceRegistrationService, asyncServiceProvider, threadingContext, diagnosticsClientName: null)
        {
        }

        /// <summary>
        /// Gets the name of the language client (displayed to the user).
        /// </summary>
        public override string Name => Resources.Xaml_Language_Server_Client;

        protected internal override VSServerCapabilities GetCapabilities()
            => new VSServerCapabilities
            {
                CompletionProvider = new CompletionOptions { ResolveProvider = true, TriggerCharacters = new string[] { "<", " ", ":", ".", "=", "\"", "'", "{", ",", "(" } },
                HoverProvider = true,
                FoldingRangeProvider = new FoldingRangeOptions { },
                DocumentFormattingProvider = true,
                DocumentRangeFormattingProvider = true,
                DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions { FirstTriggerCharacter = ">", MoreTriggerCharacter = new string[] { "\n" } },
                OnAutoInsertProvider = new DocumentOnAutoInsertOptions { TriggerCharacters = new[] { "=", "/", ">" } },
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    Change = TextDocumentSyncKind.None,
                    OpenClose = false
                },
                SupportsDiagnosticRequests = true,
            };
    }
}
