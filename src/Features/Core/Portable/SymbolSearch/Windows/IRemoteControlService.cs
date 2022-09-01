// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
