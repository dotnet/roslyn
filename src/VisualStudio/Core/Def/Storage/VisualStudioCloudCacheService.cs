// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.VisualStudio.RpcContracts.Caching;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    /// <summary>
    /// Tiny wrappers that takes the platform <see cref="ICacheService"/> and wraps it to our own layers as an <see
    /// cref="ICloudCacheService"/>.
    /// </summary>
    internal class VisualStudioCloudCacheService : AbstractCloudCacheService
    {
        private readonly IThreadingContext _threadingContext;

        public VisualStudioCloudCacheService(IThreadingContext threadingContext, ICacheService cacheService)
            : base(cacheService)
        {
            _threadingContext = threadingContext;
        }

        public override void Dispose()
        {
            if (this.CacheService is IAsyncDisposable asyncDisposable)
            {
                _threadingContext.JoinableTaskFactory.Run(
                    async () => await asyncDisposable.DisposeAsync().ConfigureAwait(false));
            }
            else if (this.CacheService is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
