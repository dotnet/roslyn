// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.Mocks;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    public class AssetStorageTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestCreation()
        {
            var storage = new AssetStorage();
            var source = new TestAssetSource(storage);

            var stored = storage.AssetSource;
            Assert.Equal(source, stored);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestGetAssets()
        {
            var storage = new AssetStorage();

            var checksum = Checksum.Create(WellKnownSynchronizationKind.Null, ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var data = new object();

            Assert.True(storage.TryAddAsset(checksum, data));

            Assert.True(storage.TryGetAsset(checksum, out object stored));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestCleanup()
        {
            var storage = new AssetStorage(cleanupInterval: TimeSpan.FromMilliseconds(1), purgeAfter: TimeSpan.FromMilliseconds(2), gcAfter: TimeSpan.FromMilliseconds(5));

            var checksum = Checksum.Create(WellKnownSynchronizationKind.Null, ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var data = new object();

            Assert.True(storage.TryAddAsset(checksum, data));

            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(10);

                if (!storage.TryGetAsset(checksum, out object stored))
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
