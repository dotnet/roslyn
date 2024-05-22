// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host;

[Obsolete("API is no longer available")]
public interface ITemporaryStorageService : IWorkspaceService
{
    ITemporaryStreamStorage CreateTemporaryStreamStorage(CancellationToken cancellationToken = default);
    ITemporaryTextStorage CreateTemporaryTextStorage(CancellationToken cancellationToken = default);
}

internal interface ITemporaryStorageServiceInternal : IWorkspaceService
{
    ITemporaryStreamStorageInternal CreateTemporaryStreamStorage();
    ITemporaryTextStorageInternal CreateTemporaryTextStorage();
}
