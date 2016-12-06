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
    public class AssetServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestAssets()
        {
            var sessionId = 0;
            var checksum = new Checksum(Guid.NewGuid().ToByteArray());
            var data = new object();

            var storage = new AssetStorage(enableCleanup: false);
            var source = new MyAssetSource(storage, sessionId, checksum, data);

            var service = new AssetService(sessionId, storage);
            var stored = await service.GetAssetAsync<object>(checksum, CancellationToken.None);
            Assert.Equal(data, stored);

            var stored2 = await service.GetAssetsAsync<object>(new[] { checksum }, CancellationToken.None);
            Assert.Equal(1, stored2.Count);

            Assert.Equal(checksum, stored2[0].Item1);
            Assert.Equal(data, stored2[0].Item2);
        }

        private class MyAssetSource : AssetSource
        {
            private readonly Checksum _checksum;
            private readonly object _data;

            public MyAssetSource(AssetStorage assetStorage, int sessionId, Checksum checksum, object data) :
                base(assetStorage, sessionId)
            {
                _checksum = checksum;
                _data = data;
            }

            public override Task<IList<(Checksum, object)>> RequestAssetsAsync(int serviceId, ISet<Checksum> checksums, CancellationToken cancellationToken)
            {
                if (checksums.Contains(_checksum))
                {
                    return Task.FromResult<IList<(Checksum, object)>>(new List<(Checksum, object)>() { ValueTuple.Create(_checksum, _data) });
                }

                // fail
                Assert.True(false);
                return null;
            }
        }
    }
}
