using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageServerClient
{
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Export(typeof(ILanguageClient))]
    [ExportMetadata("Capability", "WorkspaceStreamingSymbolProvider")]
    internal class CSharpLanguageServerClient : ILanguageClient
    {
        /// <summary>
        /// Gets the name of the language client (displayed to the user).
        /// </summary>
        public string Name => "C# Language Server Client";

        /// <summary>
        /// Gets the configuration section names for the language client. This may be null if the language client
        /// does not provide settings.
        /// </summary>
        public IEnumerable<string> ConfigurationSections => null;

        /// <summary>
        /// Gets the initialization options object the client wants to send when 'initialize' message is sent.
        /// This may be null if the client does not need custom initialization options.
        /// </summary>
        public object InitializationOptions => null;

        /// <summary>
        /// Gets the list of file names to watch for changes.  Changes will be sent to the server via 'workspace/didChangeWatchedFiles'
        /// message.  The files to watch must be under the current active workspace.  The file names can be specified as a relative
        /// paths to the exact file, or as glob patterns following the standard in .gitignore see https://www.kernel.org/pub/software/scm/git/docs/gitignore.html files.
        /// </summary>
        public IEnumerable<string> FilesToWatch => null;

#pragma warning disable CS0067
        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;
#pragma warning restore CS0067

        [Import]
        private PrimaryWorkspace _primaryWorkspace = null;

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            // Establish a connection to the WellKnownServiceHubServices.LanguageServer
            string codeAnalysisHostGroupId = await GetHostGroupIdAsync(token).ConfigureAwait(false);
            var hostGroup = new HostGroup(codeAnalysisHostGroupId);
            var serviceDescriptor = new ServiceDescriptor(WellKnownServiceHubServices.LanguageServer) { HostGroup = hostGroup };
            var client = new HubClient("ManagedLanguage.IDE.CSharpLSPServerClient");
            var stream = await client.RequestServiceAsync(serviceDescriptor, token).ConfigureAwait(true);
            return new Connection(stream, stream);
        }

        public async Task<string> GetHostGroupIdAsync(CancellationToken cancellationToken)
        {
            var client = await _primaryWorkspace.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                // exception is handled by code lens engine
                throw new InvalidOperationException("remote host doesn't exist");
            }

            return client.ClientId;
        }

        /// <summary>
        /// Signals that the extension has been loaded.  The server can be started immediately, or wait for user action to start.  To start the server, invoke the <see cref="StartAsync"/> event;
        /// </summary>
        public async Task OnLoadedAsync()
        {
            await (StartAsync?.InvokeAsync(this, EventArgs.Empty)).ConfigureAwait(false);
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
