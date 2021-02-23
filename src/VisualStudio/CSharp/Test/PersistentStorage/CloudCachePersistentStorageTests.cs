// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    public class CloudCachePersistentStorageTests : AbstractPersistentStorageTests
    {
        internal override AbstractPersistentStorageService GetStorageService(
            IMefHostExportProvider exportProvider, IPersistentStorageLocationService locationService, IPersistentStorageFaultInjector? faultInjector, string relativePathBase)
        {
            var provider = new TestCloudCacheServiceProvider(
                exportProvider.GetExports<IThreadingContext>().Single().Value, relativePathBase);

            // `mustSucceed: false` is intentional.  Our tests do actually validate that if you pass invalid paths
            // around that the db fails.
            return new CloudCachePersistentStorageService(provider, locationService, mustSucceed: false);
        }
    }
}
