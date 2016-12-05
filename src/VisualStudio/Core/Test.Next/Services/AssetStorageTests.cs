// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    public class AssetStorageTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestCreation()
        {
            var sessionId = 0;

            var storage = new AssetStorage(enableCleanup: false);
            var source = new MyAssetSource(storage, sessionId);

            var stored = storage.TryGetAssetSource(sessionId);
            Assert.Equal(source, stored);

            storage.UnregisterAssetSource(sessionId);

            var none = storage.TryGetAssetSource(sessionId);
            Assert.Null(none);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestGetAssets()
        {
            var sessionId = 0;

            var storage = new AssetStorage(enableCleanup: false);
            var source = new MyAssetSource(storage, sessionId);

            var checksum = new Checksum(Guid.NewGuid().ToByteArray());
            var data = new object();

            Assert.True(storage.TryAddAsset(checksum, data));

            object stored;
            Assert.True(storage.TryGetAsset(checksum, out stored));
        }

        private class MyAssetSource : AssetSource
        {
            public MyAssetSource(AssetStorage assetStorage, int sessionId) :
                base(assetStorage, sessionId)
            {
            }

            public override Task<IList<(Checksum, object)>> RequestAssetsAsync(int serviceId, ISet<Checksum> checksums, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
