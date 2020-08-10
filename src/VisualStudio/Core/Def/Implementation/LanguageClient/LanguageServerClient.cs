// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
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
    [Export(typeof(LanguageServerClient))]
    internal sealed class LanguageServerClient : ILanguageClient
    {
        private const string ServiceHubClientName = "ManagedLanguage.IDE.LanguageServer";

        private readonly IThreadingContext _threadingContext;
        private readonly HostWorkspaceServices _services;
        private readonly IEnumerable<Lazy<IOptionPersister>> _lazyOptions;

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
            [ImportMany] IEnumerable<Lazy<IOptionPersister>> lazyOptions)
        {
            _threadingContext = threadingContext;
            _services = workspace.Services;
            _lazyOptions = lazyOptions;
        }

        public async Task<Connection> ActivateAsync(CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_services, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                // There is no OOP. either user turned it off, or process got killed.
                // We should have already gotten a gold bar + nfw already if the OOP is missing.
                // so just log telemetry here so we can connect the two with session explorer.
                Logger.Log(FunctionId.LanguageServer_ActivateFailed, KeyValueLogMessage.NoProperty);
                return null;
            }

            var hostGroup = new HostGroup(client.ClientId);
            var hubClient = new HubClient(ServiceHubClientName);

            var stream = await ServiceHubRemoteHostClient.RequestServiceAsync(
                _services,
                hubClient,
                WellKnownServiceHubService.LanguageServer,
                hostGroup,
                cancellationToken).ConfigureAwait(false);

            return new Connection(stream, stream);
        }

        /// <summary>
        /// Signals that the extension has been loaded.
        /// The caller expects that <see cref="ActivateAsync(CancellationToken)"/> can be called
        /// immediately following the completion of this method.
        /// </summary>
        public async Task OnLoadedAsync()
        {
            // initialize things on UI thread
            await InitializeOnUIAsync().ConfigureAwait(false);

            // let platform know that they can start us
            await StartAsync.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);

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
                var experimentationService = _services.GetService<IExperimentationService>();
            }
        }

        /// <summary>
        /// Signals the extension that the language server has been successfully initialized.
        /// </summary>
        public Task OnServerInitializedAsync()
            => Task.CompletedTask;

        /// <summary>
        /// Signals the extension that the language server failed to initialize.
        /// </summary>
        public Task OnServerInitializeFailedAsync(Exception e)
            => Task.CompletedTask;
    }
}
