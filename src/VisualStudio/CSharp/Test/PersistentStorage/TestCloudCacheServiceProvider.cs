// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks;
using Microsoft.VisualStudio.Cache;
using Microsoft.VisualStudio.LanguageServices.Storage;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    internal class TestCloudCacheServiceProvider : AbstractMockRoslynCloudCacheServiceProvider
    {
        private readonly IThreadingContext _threadingContext;

        public TestCloudCacheServiceProvider(IThreadingContext threadingContext, string relativePathBase)
            : base(relativePathBase)
        {
            Console.WriteLine($"Instantiated {nameof(TestCloudCacheServiceProvider)}");
            _threadingContext = threadingContext;
        }

        protected override IRoslynCloudCacheService CreateService(CacheService cacheService)
            => new VisualStudioCloudCacheService(_threadingContext, cacheService);
    }
}
