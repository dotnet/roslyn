// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// This service allows you to access temporary storage.
    /// </summary>
    public interface ITemporaryStorageService : IWorkspaceService
    {
        ITemporaryStreamStorage CreateTemporaryStreamStorage(CancellationToken cancellationToken = default);
        ITemporaryTextStorage CreateTemporaryTextStorage(CancellationToken cancellationToken = default);
    }
}
