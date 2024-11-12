// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SymbolSearch;

/// <summary>
/// Used so we can mock out the remote control service in unit tests.
/// </summary>
internal interface IFileDownloaderFactory
{
    IFileDownloader CreateClient(string hostId, string serverPath, int pollingMinutes);
}

internal interface IFileDownloader : IDisposable
{
    public Task<Stream?> ReadFileAsync();
}
