// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    /// <summary>
    /// Language client responsible for handling C# / VB LSP requests in any scenario (both local and codespaces).
    /// This powers "LSP only" features (e.g. cntrl+Q code search) that do not use traditional editor APIs.
    /// It is always activated whenever roslyn is activated.
    /// </summary>
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [Export(typeof(ILanguageClient))]
    [Export(typeof(AlwaysActivateInProcLanguageClient))]
    internal class AlwaysActivateInProcLanguageClient : AbstractInProcLanguageClient
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public AlwaysActivateInProcLanguageClient(
            LanguageServerProtocol languageServerProtocol,
            VisualStudioWorkspace workspace,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            [Import(typeof(SAsyncServiceProvider))] VSShell.IAsyncServiceProvider asyncServiceProvider,
            IThreadingContext threadingContext)
            : base(languageServerProtocol, workspace, diagnosticService: null, listenerProvider, lspWorkspaceRegistrationService, asyncServiceProvider, threadingContext, diagnosticsClientName: null)
        {
        }

        public override string Name
            => ServicesVSResources.CSharp_Visual_Basic_Language_Server_Client;

        protected internal override VSServerCapabilities GetCapabilities()
        {
            var experimentationService = Workspace.Services.GetRequiredService<IExperimentationService>();
            var isTextSyncEnabled = experimentationService.IsExperimentEnabled(WellKnownExperimentNames.LspTextSyncEnabled);

            return new VSServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    Change = isTextSyncEnabled ? TextDocumentSyncKind.Incremental : TextDocumentSyncKind.None,
                    OpenClose = isTextSyncEnabled,
                },
                SupportsDiagnosticRequests = this.Workspace.IsPullDiagnostics(InternalDiagnosticsOptions.NormalDiagnosticMode),
                // This flag ensures that ctrl+, search locally uses the old editor APIs so that only ctrl+Q search is powered via LSP.
                DisableGoToWorkspaceSymbols = true,
                WorkspaceSymbolProvider = true,
            };
        }
    }
}
