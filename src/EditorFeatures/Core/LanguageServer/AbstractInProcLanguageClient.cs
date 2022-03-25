// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;
using Nerdbank.Streams;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient
{
    internal abstract partial class AbstractInProcLanguageClient : ILanguageClient, ILanguageServerFactory, ICapabilitiesProvider
    {
        private readonly string? _diagnosticsClientName;
        private readonly IThreadingContext _threadingContext;
        private readonly ILspLoggerFactory _lspLoggerFactory;

        private readonly IAsynchronousOperationListenerProvider _listenerProvider;
        private readonly AbstractRequestDispatcherFactory _requestDispatcherFactory;
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;

        protected readonly IGlobalOptionService GlobalOptions;

        /// <summary>
        /// Created when <see cref="ActivateAsync"/> is called.
        /// </summary>
        private LanguageServerTarget? _languageServer;

        /// <summary>
        /// Gets the name of the language client (displayed to the user).
        /// </summary>
        public string Name => ServerKind.ToUserVisibleString();

        /// <summary>
        /// An enum representing this server instance.
        /// </summary>
        public abstract WellKnownLspServerKinds ServerKind { get; }

        /// <summary>
        /// The set of languages that this LSP server supports and can return results for.
        /// </summary>
        protected abstract ImmutableArray<string> SupportedLanguages { get; }

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
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            ILspLoggerFactory lspLoggerFactory,
            IThreadingContext threadingContext,
            string? diagnosticsClientName)
        {
            _requestDispatcherFactory = requestDispatcherFactory;
            GlobalOptions = globalOptions;
            _listenerProvider = listenerProvider;
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _diagnosticsClientName = diagnosticsClientName;
            _lspLoggerFactory = lspLoggerFactory;
            _threadingContext = threadingContext;
        }

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

            // To help mitigate some of the issues with this hack we first allow implementors to do some work
            // so they can do MEF part loading before the UI thread switch. This doesn't help with the options
            // persisters, but at least doesn't make it worse.
            Activate_OffUIThread();

            // Now switch and do the problematic GetCapabilities call
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            _ = GetCapabilities(new VSInternalClientCapabilities { SupportsVisualStudioExtensions = true });

            if (_languageServer is not null)
            {
                Contract.ThrowIfFalse(_languageServer.HasShutdownStarted, "The language server has not yet been asked to shutdown.");

                await _languageServer.DisposeAsync().ConfigureAwait(false);
            }

            var (clientStream, serverStream) = FullDuplexStream.CreatePair();

            _languageServer = (LanguageServerTarget)await CreateAsync(
                this,
                serverStream,
                serverStream,
                _lspLoggerFactory,
                _diagnosticsClientName,
                cancellationToken).ConfigureAwait(false);

            return new Connection(clientStream, clientStream);
        }

        protected virtual void Activate_OffUIThread()
        {
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

        internal static async Task<ILanguageServerTarget> CreateAsync(
            AbstractInProcLanguageClient languageClient,
            Stream inputStream,
            Stream outputStream,
            ILspLoggerFactory lspLoggerFactory,
            string? clientName,
            CancellationToken cancellationToken)
        {
            var jsonMessageFormatter = new JsonMessageFormatter();
            VSInternalExtensionUtilities.AddVSInternalExtensionConverters(jsonMessageFormatter.JsonSerializer);

            var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(outputStream, inputStream, jsonMessageFormatter))
            {
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            var serverTypeName = languageClient.GetType().Name;

            var logger = await lspLoggerFactory.CreateLoggerAsync(serverTypeName, clientName, jsonRpc, cancellationToken).ConfigureAwait(false);

            var server = languageClient.Create(
                jsonRpc,
                languageClient,
                logger);

            jsonRpc.StartListening();
            return server;
        }

        public ILanguageServerTarget Create(
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            ILspLogger logger)
        {
            return new LanguageServerTarget(
                _requestDispatcherFactory,
                jsonRpc,
                capabilitiesProvider,
                _lspWorkspaceRegistrationService,
                lspMiscellaneousFilesWorkspace: null,
                GlobalOptions,
                _listenerProvider,
                logger,
                SupportedLanguages,
                clientName: _diagnosticsClientName,
                ServerKind);
        }

        public abstract ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities);

        public Task<InitializationFailureContext?> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        {
            var initializationFailureContext = new InitializationFailureContext();
            initializationFailureContext.FailureMessage = string.Format(EditorFeaturesResources.Language_client_initialization_failed,
                Name, initializationState.StatusMessage, initializationState.InitializationException?.ToString());
            return Task.FromResult<InitializationFailureContext?>(initializationFailureContext);
        }

        public abstract bool ShowNotificationOnInitializeFailed { get; }
    }
}
