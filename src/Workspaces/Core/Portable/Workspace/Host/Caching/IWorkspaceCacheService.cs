// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.Host;

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
