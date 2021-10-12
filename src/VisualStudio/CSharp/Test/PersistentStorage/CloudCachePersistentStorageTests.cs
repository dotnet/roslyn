// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    public class CloudCachePersistentStorageTests : AbstractPersistentStorageTests
    {
        internal override AbstractPersistentStorageService GetStorageService(
            IMefHostExportProvider exportProvider, IPersistentStorageConfiguration configuration, IPersistentStorageFaultInjector? faultInjector, string relativePathBase)
        {
            return new MockCloudCachePersistentStorageService(configuration, relativePathBase);
        }
    }
}
