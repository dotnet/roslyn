// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PersistentStorage;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// This service allows you to persist information relative to solution, projects and documents.
    /// </summary>
    public interface IPersistentStorageService : IWorkspaceService
    {
        [Obsolete("Use GetStorageAsync instead", error: false)]
        IPersistentStorage GetStorage(Solution solution);
        ValueTask<IPersistentStorage> GetStorageAsync(Solution solution, CancellationToken cancellationToken = default);
    }
}
