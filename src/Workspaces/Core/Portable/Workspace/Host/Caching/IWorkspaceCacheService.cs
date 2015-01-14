// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Workspace service for cache implementations.
    /// </summary>
    internal interface IWorkspaceCacheService : IWorkspaceService
    {
        /// <summary>
        /// May be raised by a Workspace host when available memory is getting low in order to request
        /// that caches be flushed.
        /// </summary>
        event EventHandler CacheFlushRequested;
    }
}
