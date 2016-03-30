// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.RemoteControl;
using VSRemoteControlClient = Microsoft.VisualStudio.RemoteControl.RemoteControlClient;

namespace Microsoft.CodeAnalysis.HubServices.SymbolSearch
{
    public partial class SymbolSearchController
    {
        private class RemoteControlService : ISymbolSearchRemoteControlService
        {
            private const string BaseUrl = "https://az700632.vo.msecnd.net/pub";

            public RemoteControlService()
            {
            }

            public ISymbolSearchRemoteControlClient CreateClient(string hostId, string serverPath, int pollingMinutes)
            {
                return new RemoteControlClient(
                    new VSRemoteControlClient(
                        hostId, BaseUrl, serverPath, pollingIntervalMins: pollingMinutes));
            }
        }

        private class RemoteControlClient : ISymbolSearchRemoteControlClient
        {
            private readonly VSRemoteControlClient _client;

            public RemoteControlClient(VSRemoteControlClient client)
            {
                _client = client;
            }

            public void Dispose()
            {
                _client.Dispose();
            }

            public Task<Stream> ReadFileAsync(BehaviorOnStale behavior)
            {
                return _client.ReadFileAsync(behavior);
            }
        }
    }
}
