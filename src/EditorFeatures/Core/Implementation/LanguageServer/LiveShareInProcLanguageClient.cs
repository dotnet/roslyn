// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient
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
            RequestDispatcherFactory csharpVBRequestDispatcherFactory,
            IGlobalOptionService globalOptions,
            IDiagnosticService diagnosticService,
            IAsynchronousOperationListenerProvider listenerProvider,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            DefaultCapabilitiesProvider defaultCapabilitiesProvider,
            ILspLoggerFactory lspLoggerFactory,
            IThreadingContext threadingContext)
            : base(csharpVBRequestDispatcherFactory, globalOptions, diagnosticService, listenerProvider, lspWorkspaceRegistrationService, lspLoggerFactory, threadingContext, diagnosticsClientName: null)
        {
            _defaultCapabilitiesProvider = defaultCapabilitiesProvider;
        }

        protected override ImmutableArray<string> SupportedLanguages => ProtocolConstants.RoslynLspLanguages;

        public override string Name => "Live Share C#/Visual Basic Language Server Client";

        public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
        {
            var isLspEditorEnabled = GlobalOptions.GetOption(LspOptions.LspEditorFeatureFlag);

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

        /// <summary>
        /// Failures are catastrophic as liveshare guests will not have language features without this server.
        /// </summary>
        public override bool ShowNotificationOnInitializeFailed => true;
    }
}
