using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Remote;
using StreamJsonRpc;
using Microsoft.ServiceHub.Client;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageServerClient
{
    [ContentType("CSharp")]
    [Export(typeof(ILanguageClient))]
    [ExportMetadata("Capability", "workspace/beginSymbol")]
    internal class CSharpLanguageServerClient : ILanguageClient
    {
        public string Name => "C# Language Server Client";
        public IEnumerable<string> ConfigurationSections => new string[] { "C#" };
        public object InitializationOptions => null;
        public IEnumerable<string> FilesToWatch => new string[] { "**/*.cs" };

        public event AsyncEventHandler<EventArgs> StartAsync;
#pragma warning disable CS0067
        public event AsyncEventHandler<EventArgs> StopAsync;
#pragma warning restore CS0067

        [Import]
        private PrimaryWorkspace _primaryWorkspace = null;

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
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

        public async Task OnLoadedAsync()
        {
            await (StartAsync?.InvokeAsync(this, EventArgs.Empty)).ConfigureAwait(false);
        }

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return Task.CompletedTask;
        }
    }
}
