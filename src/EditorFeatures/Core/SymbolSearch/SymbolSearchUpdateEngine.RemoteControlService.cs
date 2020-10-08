// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.RemoteControl;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal partial class SymbolSearchUpdateEngine
    {
        private class RemoteControlService : IRemoteControlService
        {
            public IRemoteControlClient CreateClient(string hostId, string serverPath, int pollingMinutes)
            {
                // BaseUrl provided by the VS RemoteControl client team.  This is URL we are supposed
                // to use to publish and access data from.
                const string BaseUrl = "https://az700632.vo.msecnd.net/pub";

                return new RemoteControlClient(hostId, BaseUrl, serverPath, pollingMinutes);
            }
        }
    }
}
