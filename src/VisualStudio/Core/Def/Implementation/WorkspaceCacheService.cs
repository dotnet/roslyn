// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices;

[ExportWorkspaceService(typeof(IWorkspaceCacheService), ServiceLayer.Host), Shared]
internal sealed class WorkspaceCacheService : IWorkspaceCacheService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WorkspaceCacheService()
    {
    }

    /// <summary>
    /// Called by the host to try and reduce memory occupied by caches.
    /// </summary>
    public void FlushCaches()
        => this.CacheFlushRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Raised by the host when available memory is getting low in order to request that caches be flushed.
    /// </summary>
    public event EventHandler CacheFlushRequested;
}
