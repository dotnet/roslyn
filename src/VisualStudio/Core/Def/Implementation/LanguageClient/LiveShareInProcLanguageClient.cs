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
    // The C# and VB ILanguageClient should not activate on the host. When LiveShare mirrors the C# ILC to the guest,
    // they will not copy the DisableUserExperience attribute, so guests will still use the C# ILC.
    [DisableUserExperience(true)]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [Export(typeof(ILanguageClient))]
    internal class LiveShareInProcLanguageClient : AbstractInProcLanguageClient
    {
        private readonly DefaultCapabilitiesProvider _defaultCapabilitiesProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public LiveShareInProcLanguageClient(
            CSharpVisualBasicRequestDispatcherFactory csharpVBRequestDispatcherFactory,
            VisualStudioWorkspace workspace,
            IDiagnosticService diagnosticService,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            DefaultCapabilitiesProvider defaultCapabilitiesProvider,
            [Import(typeof(SAsyncServiceProvider))] VSShell.IAsyncServiceProvider asyncServiceProvider,
            IThreadingContext threadingContext)
            : base(csharpVBRequestDispatcherFactory, workspace, diagnosticService, listenerProvider, lspWorkspaceRegistrationService, asyncServiceProvider, threadingContext, diagnosticsClientName: null)
        {
            _defaultCapabilitiesProvider = defaultCapabilitiesProvider;
        }

        public override string Name => "Live Share C#/Visual Basic Language Server Client";

        public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
        {
            var experimentationService = Workspace.Services.GetRequiredService<IExperimentationService>();
            var isLspEditorEnabled = experimentationService.IsExperimentEnabled(VisualStudioWorkspaceContextService.LspEditorFeatureFlagName);

            // If the preview feature flag to turn on the LSP editor in local scenarios is on, advertise no capabilities for this Live Share
            // LSP server as LSP requests will be serviced by the AlwaysActiveInProcLanguageClient in both local and remote scenarios.
            if (isLspEditorEnabled)
            {
                return new VSServerCapabilities
                {
                    TextDocumentSync = new TextDocumentSyncOptions
                    {
                        OpenClose = false,
                        Change = TextDocumentSyncKind.None,
                    }
                };
            }

            return _defaultCapabilitiesProvider.GetCapabilities(clientCapabilities);
        }
    }
}
