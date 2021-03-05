// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.VisualStudio.RpcContracts.Caching;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    internal class VisualStudioCloudCachePersistentStorage : AbstractCloudCachePersistentStorage
    {
        private readonly IThreadingContext _threadingContext;

        public VisualStudioCloudCachePersistentStorage(
            IThreadingContext threadingContext,
            ICacheService cacheService,
            SolutionKey solutionKey,
            string workingFolderPath,
            string relativePathBase,
            string databaseFilePath)
            : base(cacheService, solutionKey, workingFolderPath, relativePathBase, databaseFilePath)
        {
            _threadingContext = threadingContext;
        }

        public override void Dispose()
        {
            if (this.CacheService is IAsyncDisposable asyncDisposable)
            {
                _threadingContext.JoinableTaskFactory.Run(
                    () => asyncDisposable.DisposeAsync().AsTask());
            }
            else if (this.CacheService is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
