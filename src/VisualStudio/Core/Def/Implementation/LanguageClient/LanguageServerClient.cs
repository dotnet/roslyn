// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.ServiceHub.Client;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [Export(typeof(ILanguageClient))]
    [ExportMetadata("Capabilities", "WorkspaceStreamingSymbolProvider")]
    internal sealed class LanguageServerClient : ILanguageClient
    {
        private const string ServiceHubClientName = "ManagedLanguage.IDE.LanguageServer";

        private readonly IThreadingContext _threadingContext;
        private readonly Workspace _workspace;
        private readonly IEnumerable<Lazy<IOptionPersister>> _lazyOptions;
        private readonly LanguageServerClientEventListener _eventListener;
        private readonly IAsynchronousOperationListener _asyncListener;

        /// <summary>
        /// Gets the name of the language client (displayed to the user).
        /// </summary>
        public string Name => ServicesVSResources.CSharp_Visual_Basic_Language_Server_Client;

        /// <summary>
        /// Gets the configuration section names for the language client. This may be null if the language client
        /// does not provide settings.
        /// </summary>
        public IEnumerable<string> ConfigurationSections { get; }

        /// <summary>
        /// Gets the initialization options object the client wants to send when 'initialize' message is sent.
        /// This may be null if the client does not need custom initialization options.
        /// </summary>
        public object InitializationOptions { get; }

        /// <summary>
        /// Gets the list of file names to watch for changes.  Changes will be sent to the server via 'workspace/didChangeWatchedFiles'
        /// message.  The files to watch must be under the current active workspace.  The file names can be specified as a relative
        /// paths to the exact file, or as glob patterns following the standard in .gitignore see https://www.kernel.org/pub/software/scm/git/docs/gitignore.html files.
        /// </summary>
        public IEnumerable<string> FilesToWatch { get; }

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync { add { } remove { } }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LanguageServerClient(
            IThreadingContext threadingContext,
            VisualStudioWorkspace workspace,
            [ImportMany]IEnumerable<Lazy<IOptionPersister>> lazyOptions,
            LanguageServerClientEventListener eventListener,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _workspace = workspace;
            _lazyOptions = lazyOptions;
            _eventListener = eventListener;
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.LanguageServerWorkspaceSymbolSearch);
        }

        public async Task<Connection> ActivateAsync(CancellationToken cancellationToken)
        {
            var client = await _workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                // there is no OOP. either user turned it off, or process got killed.
                return null;
            }

            var hostGroup = new HostGroup(client.ClientId);
            var hubClient = new HubClient(ServiceHubClientName);

            var stream = await ServiceHubRemoteHostClient.Connections.RequestServiceAsync(
                _workspace,
                hubClient,
                WellKnownServiceHubServices.LanguageServer,
                hostGroup,
                TimeSpan.FromMinutes(60),
                cancellationToken).ConfigureAwait(false);

            return new Connection(stream, stream);
        }

        /// <summary>
        /// Signals that the extension has been loaded.  The server can be started immediately, or wait for user action to start.  
        /// To start the server, invoke the <see cref="StartAsync"/> event;
        /// </summary>
        public Task OnLoadedAsync()
        {
            var token = _asyncListener.BeginAsyncOperation("OnLoadedAsync");

            // set up event stream so that we start LSP server once Roslyn is loaded
            _eventListener.WorkspaceStarted.ContinueWith(async _ =>
            {
                // initialize things on UI thread
                await InitializeOnUIAsync().ConfigureAwait(false);

                // this might get called before solution is fully loaded and before file is opened. 
                // we delay our OOP start until then, but user might do vsstart before that. so we make sure we start OOP if 
                // it is not running yet. multiple start is no-op
                ((RemoteHostClientServiceFactory.RemoteHostClientService)_workspace.Services.GetService<IRemoteHostClientService>()).Enable();

                // wait until remote host is available before let platform know that they can activate our LSP
                var client = await _workspace.TryGetRemoteHostClientAsync(CancellationToken.None).ConfigureAwait(false);
                if (client == null)
                {
                    // there is no OOP. either user turned it off, or process got killed.
                    // don't ask platform to start LSP
                    return;
                }

                // let platform know that they can start us
                await StartAsync.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
            }, TaskScheduler.Default).CompletesAsyncOperation(token);

            return Task.CompletedTask;

            async Task InitializeOnUIAsync()
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                // this doesn't attempt to solve our JTF and some services being not free-thread issue here, but
                // try to fix this particular deadlock issue only. we already have long discussion on
                // how we need to deal with JTF, Roslyn service requirements and VS services reality conflicting
                // each others. architectural fix should come from the result of that discussion.

                // Ensure the options persisters are loaded since we have to fetch options from the shell
                _lazyOptions.Select(o => o.Value);

                // experimentation service unfortunately uses JTF to jump to UI thread in certain cases
                // which can cause deadlock if 2 parties try to enable OOP from BG and then FG before 
                // experimentation service tries to jump to UI thread.
                var experimentationService = _workspace.Services.GetService<IExperimentationService>();
            }
        }

        /// <summary>
        /// Signals the extension that the language server has been successfully initialized.
        /// </summary>
        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Signals the extension that the language server failed to initialize.
        /// </summary>
        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return Task.CompletedTask;
        }
    }
}
