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
        private readonly DefaultCapabilitiesProvider _defaultCapabilitiesProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public AlwaysActivateInProcLanguageClient(
            CSharpVisualBasicRequestDispatcherFactory csharpVBRequestDispatcherFactory,
            VisualStudioWorkspace workspace,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            DefaultCapabilitiesProvider defaultCapabilitiesProvider,
            [Import(typeof(SAsyncServiceProvider))] VSShell.IAsyncServiceProvider asyncServiceProvider,
            IThreadingContext threadingContext)
            : base(csharpVBRequestDispatcherFactory, workspace, diagnosticService: null, listenerProvider, lspWorkspaceRegistrationService, asyncServiceProvider, threadingContext, diagnosticsClientName: null)
        {
            _defaultCapabilitiesProvider = defaultCapabilitiesProvider;
        }

        public override string Name => CSharpVisualBasicLanguageServerFactory.UserVisibleName;

        public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
        {
            var serverCapabilities = new VSServerCapabilities();

            // If the LSP editor feature flag is enabled advertise support for LSP features here so they are available locally and remote.
            var isLspEditorEnabled = Workspace.Services.GetRequiredService<IExperimentationService>().IsExperimentEnabled(VisualStudioWorkspaceContextService.LspEditorFeatureFlagName);
            if (isLspEditorEnabled)
            {
                serverCapabilities = (VSServerCapabilities)_defaultCapabilitiesProvider.GetCapabilities(clientCapabilities);
            }
            else
            {
                // Even if the flag is off, we want to include text sync capabilities.
                serverCapabilities.TextDocumentSync = new TextDocumentSyncOptions
                {
                    Change = TextDocumentSyncKind.Incremental,
                    OpenClose = true,
                };
            }

            serverCapabilities.SupportsDiagnosticRequests = Workspace.IsPullDiagnostics(InternalDiagnosticsOptions.NormalDiagnosticMode);

            // This capability is always enabled as we provide cntrl+Q VS search only via LSP in ever scenario.
            serverCapabilities.WorkspaceSymbolProvider = true;
            // This capability prevents NavigateTo (cntrl+,) from using LSP symbol search when the server also supports WorkspaceSymbolProvider.
            // Since WorkspaceSymbolProvider=true always to allow cntrl+Q VS search to function, we set DisableGoToWorkspaceSymbols=true
            // when not running the experimental LSP editor.  This ensures NavigateTo uses the existing editor APIs.
            // However, when the experimental LSP editor is enabled we want LSP to power NavigateTo, so we set DisableGoToWorkspaceSymbols=false.
            serverCapabilities.DisableGoToWorkspaceSymbols = !isLspEditorEnabled;

            return serverCapabilities;
        }
    }
}
