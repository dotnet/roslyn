// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.RemoteControl;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    /// <summary>
    /// Used so we can mock out the remote control service in unit tests.
    /// </summary>
    internal interface IRemoteControlService
    {
        IRemoteControlClient CreateClient(string hostId, string serverPath, int pollingMinutes);
    }
}
