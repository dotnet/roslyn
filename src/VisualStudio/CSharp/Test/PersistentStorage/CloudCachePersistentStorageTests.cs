﻿// Licensed to the .NET Foundation under one or more agreements.
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
            OptionSet options, IMefHostExportProvider exportProvider, IPersistentStorageLocationService locationService, IPersistentStorageFaultInjector? faultInjector, string relativePathBase)
        {
            var threadingContext = exportProvider.GetExports<IThreadingContext>().Single().Value;
            return new MockCloudCachePersistentStorageService(
                locationService,
                relativePathBase,
                cs =>
                {
                    if (cs is IAsyncDisposable asyncDisposable)
                    {
                        threadingContext.JoinableTaskFactory.Run(
                            () => asyncDisposable.DisposeAsync().AsTask());
                    }
                    else if (cs is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                });
        }
    }
}
