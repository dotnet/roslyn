// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                return new RemoteControlClient(vsClient, client);
            }
        }

        private class RemoteControlClient : IPackageSearchRemoteControlClient
        {
            // Have to keep the vsClient around as it will try to dispose the underlying
            // client when it gets GC'ed
            private readonly object _vsClient;
            private readonly IDisposable _client;

            public RemoteControlClient(object vsClient, IDisposable client)
            {
                _vsClient = vsClient;
                _client = client;
            }

            public void Dispose()
            {
                _client.Dispose();
            }

            public Task<Stream> ReadFileAsync(__VsRemoteControlBehaviorOnStale behavior)
            {
                var clientType = _client.GetType();
                var readFileAsyncMethod = clientType.GetMethod("ReadFileAsync");
                var streamTask = (Task<Stream>)readFileAsyncMethod.Invoke(_client, new object[] { (int)behavior });

                return streamTask;
            }
        }
    }
}
