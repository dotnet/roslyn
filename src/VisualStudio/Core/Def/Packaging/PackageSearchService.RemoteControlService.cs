using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageSearchService
    {
        private class RemoteControlService : IPackageSearchRemoteControlService
        {
            private readonly object _remoteControlService;

            public RemoteControlService(object remoteControlService)
            {
                _remoteControlService = remoteControlService;
            }

            public IPackageSearchRemoteControlClient CreateClient(string hostId, string serverPath, int pollingMinutes)
            {
                var serviceType = _remoteControlService.GetType();
                var serviceAssembly = serviceType.Assembly;
                var clientType = serviceAssembly.GetType("Microsoft.VisualStudio.Services.RemoteControl.VSRemoteControlClient");

                var vsClient = Activator.CreateInstance(clientType, args: new object[]
                {
                    HostId,
                    serverPath,
                    pollingMinutes,
                    null
                });

                var clientField = vsClient.GetType().GetField("client", BindingFlags.Instance | BindingFlags.NonPublic);
                var client = clientField.GetValue(vsClient) as IDisposable;
                return new RemoteControlClient(client);
            }
        }

        private class RemoteControlClient : IPackageSearchRemoteControlClient
        {
            private readonly IDisposable client;

            public RemoteControlClient(IDisposable client)
            {
                this.client = client;
            }

            public void Dispose()
            {
                client.Dispose();
            }

            public Task<Stream> ReadFileAsync(__VsRemoteControlBehaviorOnStale behavior)
            {
                var clientType = client.GetType();
                var readFileAsyncMethod = clientType.GetMethod("ReadFileAsync");
                var streamTask = (Task<Stream>)readFileAsyncMethod.Invoke(client, new object[] { (int)behavior });

                return streamTask;
            }
        }
    }
}
