// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.Mocks;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    public class AssetStorageTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestCreation()
        {
            var sessionId = 0;

            var storage = new AssetStorage();
            var source = new TestAssetSource(storage, sessionId);

            var stored = storage.TryGetAssetSource(sessionId);
            Assert.Equal(source, stored);

            storage.UnregisterAssetSource(sessionId);

            var none = storage.TryGetAssetSource(sessionId);
            Assert.Null(none);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestGetAssets()
        {
            var storage = new AssetStorage();

            var checksum = new Checksum(Guid.NewGuid().ToByteArray());
            var data = new object();

            Assert.True(storage.TryAddAsset(checksum, data));

            object stored;
            Assert.True(storage.TryGetAsset(checksum, out stored));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestCleanup()
        {
            var storage = new AssetStorage(cleanupInterval: TimeSpan.FromMilliseconds(1), purgeAfter: TimeSpan.FromMilliseconds(2));

            var checksum = new Checksum(Guid.NewGuid().ToByteArray());
            var data = new object();

            Assert.True(storage.TryAddAsset(checksum, data));

            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(10);

                object stored;
                if (!storage.TryGetAsset(checksum, out stored))
                {
                    // asset is deleted
                    return;
                }
            }

            // it should not reach here
            Assert.True(false, "asset not cleaned up");
        }
    }
}
