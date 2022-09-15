// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Storage
{
    /// <summary>
    /// Factory for SQLite storage service - intentionally not included under SQLite directory since all sources under it are excluded from source build.
    /// </summary>
    internal interface ISQLiteStorageServiceFactory : IWorkspaceService
    {
        IChecksummedPersistentStorageService Create(IPersistentStorageConfiguration configuration);
    }
}
