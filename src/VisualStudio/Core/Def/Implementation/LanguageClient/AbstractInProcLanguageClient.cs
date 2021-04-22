﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;
using Nerdbank.Streams;
using Roslyn.Utilities;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    internal abstract class AbstractInProcLanguageClient : ILanguageClient
    {
        private readonly string? _diagnosticsClientName;
        private readonly VSShell.IAsyncServiceProvider _asyncServiceProvider;
        private readonly IThreadingContext _threadingContext;

        /// <summary>
        /// Legacy support for LSP push diagnostics.
        /// </summary>
        private readonly IDiagnosticService? _diagnosticService;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;
        private readonly AbstractRequestDispatcherFactory _requestDispatcherFactory;
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
            AbstractRequestDispatcherFactory requestDispatcherFactory,
            VisualStudioWorkspace workspace,
            IDiagnosticService? diagnosticService,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            VSShell.IAsyncServiceProvider asyncServiceProvider,
            IThreadingContext threadingContext,
            string? diagnosticsClientName)
        {
            _requestDispatcherFactory = requestDispatcherFactory;
            Workspace = workspace;
            _diagnosticService = diagnosticService;
            _listenerProvider = listenerProvider;
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _diagnosticsClientName = diagnosticsClientName;
            _asyncServiceProvider = asyncServiceProvider;
            _threadingContext = threadingContext;
        }

        /// <summary>
        /// Can be overridden by subclasses to control what capabilities this language client has.
        /// </summary>
        protected internal abstract VSServerCapabilities GetCapabilities();

        public async Task<Connection?> ActivateAsync(CancellationToken cancellationToken)
        {
            // HACK HACK HACK: prevent potential crashes/state corruption during load. Fixes
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1261421
            //
            // When we create an LSP server, we compute our server capabilities; this may depend on
            // reading things like workspace options which will force us to initialize our option persisters.
            // Unfortunately some of our option persisters currently assert they are first created on the UI
            // thread. If the first time they're created is because of LSP initialization, we might end up loading
            // them on a background thread which will throw exceptions and then prevent them from being created
            // again later.
            //
            // The correct fix for this is to fix the threading violations in the option persister code;
            // asserting a MEF component is constructed on the foreground thread is never allowed, but alas it's
            // done there. Fixing that isn't difficult but comes with some risk I don't want to take for 16.9;
            // instead we'll just compute our capabilities here on the UI thread to ensure everything is loaded.
            // We _could_ consider doing a SwitchToMainThreadAsync in InProcLanguageServer.InitializeAsync
            // (where the problematic call to GetCapabilites is), but that call is invoked across the StreamJsonRpc
            // link where it's unclear if VS Threading rules apply. By doing this here, we are dong it in a
            // VS API that is following VS Threading rules, and it also ensures that the preereqs are loaded
            // prior to any RPC calls being made.
            //
            // https://github.com/dotnet/roslyn/issues/29602 will track removing this hack
            // since that's the primary offending persister that needs to be addressed.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            _ = GetCapabilities();

            if (_languageServer is not null)
            {
                Contract.ThrowIfFalse(_languageServer.HasShutdownStarted, "The language server has not yet been asked to shutdown.");

                await _languageServer.DisposeAsync().ConfigureAwait(false);
            }

            var (clientStream, serverStream) = FullDuplexStream.CreatePair();
            _languageServer = await InProcLanguageServer.CreateAsync(
                this,
                serverStream,
                serverStream,
                _requestDispatcherFactory.CreateRequestDispatcher(),
                Workspace,
                _diagnosticService,
                _listenerProvider,
                _lspWorkspaceRegistrationService,
                _asyncServiceProvider,
                _diagnosticsClientName,
                cancellationToken).ConfigureAwait(false);

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
