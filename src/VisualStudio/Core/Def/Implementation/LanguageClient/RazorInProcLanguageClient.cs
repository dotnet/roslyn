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
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Lsp
{
    /// <summary>
    /// Defines the LSP server for Razor C#.  This is separate so that we can
    /// activate this outside of a liveshare session and publish diagnostics
    /// only for razor cs files.
    /// TODO - This can be removed once C# is using LSP for diagnostics.
    /// https://github.com/dotnet/roslyn/issues/42630
    /// </summary>
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ClientName(ClientName)]
    [Export(typeof(ILanguageClient))]
    internal class RazorInProcLanguageClient : AbstractInProcLanguageClient
    {
        public const string ClientName = "RazorCSharp";

        private readonly IGlobalOptionService _globalOptionService;
        private readonly DefaultCapabilitiesProvider _defaultCapabilitiesProvider;

        /// <summary>
        /// Gets the name of the language client (displayed to the user).
        /// </summary>
        public override string Name => ServicesVSResources.Razor_CSharp_Language_Server_Client;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RazorInProcLanguageClient(
            IGlobalOptionService globalOptionService,
            LanguageServerProtocol languageServerProtocol,
            VisualStudioWorkspace workspace,
            IDiagnosticService diagnosticService,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            DefaultCapabilitiesProvider defaultCapabilitiesProvider)
            : base(languageServerProtocol, workspace, diagnosticService, listenerProvider, lspWorkspaceRegistrationService, ClientName)
        {
            _globalOptionService = globalOptionService;
            _defaultCapabilitiesProvider = defaultCapabilitiesProvider;
        }

        protected internal override VSServerCapabilities GetCapabilities()
        {
            var capabilities = _defaultCapabilitiesProvider.GetCapabilities();

            capabilities.SupportsDiagnosticRequests = this.Workspace.IsPullDiagnostics(InternalDiagnosticsOptions.RazorDiagnosticMode);

            // Razor doesn't use workspace symbols, so disable to prevent duplicate results (with LiveshareLanguageClient) in liveshare.
            capabilities.WorkspaceSymbolProvider = false;

            return capabilities;
        }
    }
}
