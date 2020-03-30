// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Nerdbank.Streams;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    [ContentType(ContentTypeNames.CSharpLspContentTypeName)]
    [ContentType(ContentTypeNames.VBLspContentTypeName)]
    [Export(typeof(ILanguageClient))]
    internal class LiveShareLanguageServerClient : ILanguageClient
    {
        private readonly IDiagnosticService _diagnosticService;
        private readonly LanguageServerProtocol _languageServerProtocol;
        private readonly Workspace _workspace;

        /// <summary>
        /// Gets the name of the language client (displayed to the user).
        /// </summary>
        public string Name => ServicesVSResources.Live_Share_CSharp_Visual_Basic_Language_Server_Client;

        /// <summary>
        /// Unused, implementing <see cref="ILanguageClient"/>
        /// No additional settings are provided for this server, so we do not need any configuration section names.
        /// </summary>
        public IEnumerable<string>? ConfigurationSections { get; } = null;

        /// <summary>
        /// Gets the initialization options object the client wants to send when 'initialize' message is sent.
        /// See https://microsoft.github.io/language-server-protocol/specifications/specification-3-14/#initialize
        /// We do not provide any additional initialization options.
        /// </summary>
        public object? InitializationOptions { get; } = null;

        /// <summary>
        /// Unused, implementing <see cref="ILanguageClient"/>
        /// Files that we care about are already provided and watched by the workspace.
        /// </summary>
        public IEnumerable<string>? FilesToWatch { get; } = null;

        public event AsyncEventHandler<EventArgs>? StartAsync;

        /// <summary>
        /// Unused, implementing <see cref="ILanguageClient"/>
        /// </summary>
        public event AsyncEventHandler<EventArgs>? StopAsync { add { } remove { } }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LiveShareLanguageServerClient(LanguageServerProtocol languageServerProtocol, VisualStudioWorkspace workspace, IDiagnosticService diagnosticService)
        {
            _languageServerProtocol = languageServerProtocol;
            _workspace = workspace;
            _diagnosticService = diagnosticService;
        }

        public Task<Connection> ActivateAsync(CancellationToken token)
        {
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();
            _ = new InProcLanguageServer(serverStream, serverStream, _languageServerProtocol, _workspace, _diagnosticService);
            return Task.FromResult(new Connection(clientStream, clientStream));
        }

        /// <summary>
        /// Signals that the extension has been loaded.  The server can be started immediately, or wait for user action to start.  
        /// To start the server, invoke the <see cref="StartAsync"/> event;
        /// </summary>
        public async Task OnLoadedAsync()
            => await StartAsync.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);

        /// <summary>
        /// Signals the extension that the language server has been successfully initialized.
        /// </summary>
        /// <returns>A <see cref="Task"/> which completes when actions that need to be performed when the server is ready are done.</returns>
        public Task OnServerInitializedAsync()
        {
            // We don't have any tasks that need to be triggered after the server has successfully initialized.
            return Task.CompletedTask;
        }

        /// <summary>
        /// Signals the extension that the language server failed to initialize.
        /// </summary>
        /// <returns>A <see cref="Task"/> which completes when additional actions that need to be performed when the server fails to initialize are done.</returns>
        public Task OnServerInitializeFailedAsync(Exception e)
        {
            // We don't need to provide additional exception handling here, liveshare already handles failure cases for this server.
            return Task.CompletedTask;
        }
    }
}
