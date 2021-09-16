// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
    /// <remarks>
    /// This specifies RunOnHost because in LiveShare we don't want this to activate on the guest instance
    /// because LiveShare drops the ClientName when it mirrors guest clients, so this client ends up being
    /// activated solely by its content type, which means it receives requests for normal .cs and .vb files
    /// even for non-razor projects, which then of course fails because it gets text sync info for documents
    /// it doesn't know about.
    /// </remarks>
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ClientName(ClientName)]
    [RunOnContext(RunningContext.RunOnHost)]
    [Export(typeof(ILanguageClient))]
    internal class RazorInProcLanguageClient : AbstractInProcLanguageClient
    {
        public const string ClientName = ProtocolConstants.RazorCSharp;

        private readonly DefaultCapabilitiesProvider _defaultCapabilitiesProvider;

        protected override ImmutableArray<string> SupportedLanguages => ProtocolConstants.RoslynLspLanguages;

        /// <summary>
        /// Gets the name of the language client (displayed in yellow bars).
        /// </summary>
        public override string Name => "Razor C# Language Server Client";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RazorInProcLanguageClient(
            RequestDispatcherFactory csharpVBRequestDispatcherFactory,
            IGlobalOptionService globalOptions,
            IDiagnosticService diagnosticService,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            DefaultCapabilitiesProvider defaultCapabilitiesProvider,
            IThreadingContext threadingContext,
            ILspLoggerFactory lspLoggerFactory)
            : base(csharpVBRequestDispatcherFactory, globalOptions, diagnosticService, listenerProvider, lspWorkspaceRegistrationService, lspLoggerFactory, threadingContext, ClientName)
        {
            _defaultCapabilitiesProvider = defaultCapabilitiesProvider;
        }

        public override ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
        {
            var capabilities = _defaultCapabilitiesProvider.GetCapabilities(clientCapabilities);

            // Razor doesn't use workspace symbols, so disable to prevent duplicate results (with LiveshareLanguageClient) in liveshare.
            capabilities.WorkspaceSymbolProvider = false;

            if (capabilities is VSInternalServerCapabilities vsServerCapabilities)
            {
                vsServerCapabilities.SupportsDiagnosticRequests = GlobalOptions.IsPullDiagnostics(InternalDiagnosticsOptions.RazorDiagnosticMode);
                return vsServerCapabilities;
            }

            return capabilities;
        }

        /// <summary>
        /// If the razor server is activated then any failures are catastrophic as no razor c# features will work.
        /// </summary>
        public override bool ShowNotificationOnInitializeFailed => true;
    }
}
