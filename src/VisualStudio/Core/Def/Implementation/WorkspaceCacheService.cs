// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices
{
    [ExportWorkspaceService(typeof(IWorkspaceCacheService), ServiceLayer.Host), Shared]
    internal sealed class WorkspaceCacheService : IWorkspaceCacheService
    {
        /// <summary>
        /// Called by the host to try and reduce memory occupied by caches.
        /// </summary>
        public void FlushCaches()
        {
            this.CacheFlushRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raised by the host when available memory is getting low in order to request that caches be flushed.
        /// </summary>
        public event EventHandler CacheFlushRequested;
    }
}
