// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.RemoteControl;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal partial class SymbolSearchService
    {
        private class RemoteControlService : IRemoteControlService
        {
            public IRemoteControlClient CreateClient(string hostId, string serverPath, int pollingMinutes)
            {
                const string BaseUrl = "https://az700632.vo.msecnd.net/pub";

                return new RemoteControlClient(hostId, BaseUrl, serverPath, pollingMinutes);
            }
        }
    }
}
