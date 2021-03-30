// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks;
using Xunit;

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

        [Theory]
        [CombinatorialData]
        public async Task PersistentService_ReadByteTwice(Size size, bool withChecksum)
        {
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_ReadByteTwice";

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                using var stream = await storage.ReadStreamAsync(streamName1, GetChecksum1(withChecksum));
                stream.ReadByte();
                stream.ReadByte();
            }
        }
    }
}
