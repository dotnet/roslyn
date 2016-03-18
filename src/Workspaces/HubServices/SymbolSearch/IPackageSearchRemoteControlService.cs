// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.RemoteControl;

namespace Microsoft.CodeAnalysis.HubServices.SymbolSearch
{
    /// <summary>
    /// Used so we can mock out the remote control service in unit tests.
    /// </summary>
    internal interface IPackageSearchRemoteControlService
    {
        IPackageSearchRemoteControlClient CreateClient(string hostId, string serverPath, int pollingMinutes);
    }

    /// <summary>
    /// Used so we can mock out the client in unit tests.
    /// </summary>
    internal interface IPackageSearchRemoteControlClient : IDisposable
    {
        Task<Stream> ReadFileAsync(BehaviorOnStale behavior);
    }
}
