// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Experiments;
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
        private readonly DefaultCapabilitiesProvider _defaultCapabilitiesProvider;
        private readonly IGlobalOptionService _globalOptionService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public AlwaysActivateInProcLanguageClient(
            IGlobalOptionService globalOptionService,
            CSharpVisualBasicRequestDispatcherFactory csharpVBRequestDispatcherFactory,
            VisualStudioWorkspace workspace,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            DefaultCapabilitiesProvider defaultCapabilitiesProvider)
            : base(csharpVBRequestDispatcherFactory, workspace, diagnosticService: null, listenerProvider, lspWorkspaceRegistrationService, diagnosticsClientName: null)
        {
            _globalOptionService = globalOptionService;
            _defaultCapabilitiesProvider = defaultCapabilitiesProvider;
        }

        public override string Name => "C#/Visual Basic Language Server Client";

        protected internal override VSServerCapabilities GetCapabilities()
        {
            var serverCapabilities = new VSServerCapabilities();

            // If the LSP editor feature flag is enabled advertise support for LSP features here so they are available locally and remote.
            var isLspEditorEnabled = Workspace.Services.GetRequiredService<IExperimentationService>().IsExperimentEnabled(VisualStudioWorkspaceContextService.LspEditorFeatureFlagName);
            if (isLspEditorEnabled)
            {
                serverCapabilities = _defaultCapabilitiesProvider.GetCapabilities();
            }

            serverCapabilities.TextDocumentSync = new TextDocumentSyncOptions
            {
                Change = TextDocumentSyncKind.Incremental,
                OpenClose = true,
            };
            serverCapabilities.SupportsDiagnosticRequests = Workspace.IsPullDiagnostics(InternalDiagnosticsOptions.NormalDiagnosticMode);

            // When using the lsp editor, set this to false to allow LSP to power goto.
            // Otherwise set to true to disable LSP for goto
            serverCapabilities.DisableGoToWorkspaceSymbols = !isLspEditorEnabled;
            serverCapabilities.WorkspaceSymbolProvider = true;

            return serverCapabilities;
        }
    }
}
