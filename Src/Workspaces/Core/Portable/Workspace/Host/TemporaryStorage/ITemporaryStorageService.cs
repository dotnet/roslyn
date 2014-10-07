// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// This service allows you to access temporary storage.
    /// </summary>
    public interface ITemporaryStorageService : IWorkspaceService
    {
        ITemporaryStorage CreateTemporaryStorage(CancellationToken cancellationToken = default(CancellationToken));
    }
}