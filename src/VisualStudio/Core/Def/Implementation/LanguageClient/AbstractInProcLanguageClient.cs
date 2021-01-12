// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;
using Nerdbank.Streams;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    internal abstract class AbstractInProcLanguageClient : ILanguageClient
    {
        private readonly string? _diagnosticsClientName;
        /// <summary>
        /// Legacy support for LSP push diagnostics.
        /// </summary>
        private readonly IDiagnosticService? _diagnosticService;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;
        private readonly AbstractRequestHandlerProvider _requestHandlerProvider;
        private readonly ILspWorkspaceRegistrationService _lspWorkspaceRegistrationService;

        protected readonly Workspace Workspace;

        /// <summary>
        /// Created when <see cref="ActivateAsync"/> is called.
        /// </summary>
        private InProcLanguageServer? _languageServer;

        /// <summary>
        /// Gets the name of the language client (displayed to the user).
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Unused, implementing <see cref="ILanguageClient"/>
        /// No additional settings are provided for this server, so we do not need any configuration section names.
        /// </summary>
        public IEnumerable<string>? ConfigurationSections { get; }

        /// <summary>
        /// Gets the initialization options object the client wants to send when 'initialize' message is sent.
        /// See https://microsoft.github.io/language-server-protocol/specifications/specification-3-14/#initialize
        /// We do not provide any additional initialization options.
        /// </summary>
        public object? InitializationOptions { get; }

        /// <summary>
        /// Unused, implementing <see cref="ILanguageClient"/>
        /// Files that we care about are already provided and watched by the workspace.
        /// </summary>
        public IEnumerable<string>? FilesToWatch { get; }

        public event AsyncEventHandler<EventArgs>? StartAsync;

        /// <summary>
        /// Unused, implementing <see cref="ILanguageClient"/>
        /// </summary>
        public event AsyncEventHandler<EventArgs>? StopAsync { add { } remove { } }

        public AbstractInProcLanguageClient(
            AbstractRequestHandlerProvider requestHandlerProvider,
            VisualStudioWorkspace workspace,
            IDiagnosticService? diagnosticService,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            string? diagnosticsClientName)
        {
            _requestHandlerProvider = requestHandlerProvider;
            Workspace = workspace;
            _diagnosticService = diagnosticService;
            _listenerProvider = listenerProvider;
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _diagnosticsClientName = diagnosticsClientName;
        }

        /// <summary>
        /// Can be overridden by subclasses to control what capabilities this language client has.
        /// </summary>
        protected internal abstract VSServerCapabilities GetCapabilities();

        public async Task<Connection> ActivateAsync(CancellationToken cancellationToken)
        {
            if (_languageServer is not null)
            {
                Contract.ThrowIfFalse(_languageServer.HasShutdownStarted, "The language server has not yet been asked to shutdown.");

                await _languageServer.DisposeAsync().ConfigureAwait(false);
            }

            var (clientStream, serverStream) = FullDuplexStream.CreatePair();
            _languageServer = new InProcLanguageServer(
                this,
                serverStream,
                serverStream,
                _requestHandlerProvider,
                Workspace,
                _diagnosticService,
                _listenerProvider,
                _lspWorkspaceRegistrationService,
                clientName: _diagnosticsClientName);

            return new Connection(clientStream, clientStream);
        }

        /// <summary>
        /// Signals that the extension has been loaded.  The server can be started immediately, or wait for user action to start.  
        /// To start the server, invoke the <see cref="StartAsync"/> event;
        /// </summary>
        public async Task OnLoadedAsync()
        {
            await StartAsync.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
        }

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
