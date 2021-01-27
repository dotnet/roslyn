// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Utilities;

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
        private readonly IGlobalOptionService _globalOptionService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public AlwaysActivateInProcLanguageClient(
            IGlobalOptionService globalOptionService,
            LanguageServerProtocol languageServerProtocol,
            VisualStudioWorkspace workspace,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService)
            : base(languageServerProtocol, workspace, diagnosticService: null, listenerProvider, lspWorkspaceRegistrationService, diagnosticsClientName: null)
        {
            _globalOptionService = globalOptionService;
        }

        public override string Name
            => ServicesVSResources.CSharp_Visual_Basic_Language_Server_Client;

        protected internal override VSServerCapabilities GetCapabilities()
        {
            var isPullDiagnostics = this.Workspace.IsPullDiagnostics(InternalDiagnosticsOptions.NormalDiagnosticMode);
            return new VSServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    // If pull diagnostics isn't enabled, then we don't really need text sync so can save a bunch of
                    // requests that the platform would otherwise make, and state to store all of the text documents.
                    // Strictly speaking workspace symbols could be not 100% accurate as a result, but the trade off
                    // is worth it given usage vs mainline editing scenarios
                    Change = isPullDiagnostics ? TextDocumentSyncKind.Incremental : TextDocumentSyncKind.None,
                    OpenClose = isPullDiagnostics,
                },
                SupportsDiagnosticRequests = isPullDiagnostics,
                // This flag ensures that ctrl+, search locally uses the old editor APIs so that only ctrl+Q search is powered via LSP.
                DisableGoToWorkspaceSymbols = true,
                WorkspaceSymbolProvider = true,
            };
        }
    }
}
