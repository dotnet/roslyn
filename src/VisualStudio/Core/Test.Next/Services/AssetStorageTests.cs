﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Shared;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    public class AssetStorageTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestCreation()
        {
            var storage = new AssetStorage();
            var source = new SimpleAssetSource(storage, new Dictionary<Checksum, object>());

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
