// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Mocks
{
    internal class TestAssetSource : AssetSource
    {
        private readonly Dictionary<Checksum, object> _map;

        public TestAssetSource(AssetStorage assetStorage) :
            this(assetStorage, new Dictionary<Checksum, object>())
        {
        }

        public TestAssetSource(AssetStorage assetStorage, Checksum checksum, object data) :
            this(assetStorage, new Dictionary<Checksum, object>() { { checksum, data } })
        {
        }

        public TestAssetSource(AssetStorage assetStorage, Dictionary<Checksum, object> map) :
            base(assetStorage)
        {
            _map = map;
        }

        public override Task<IList<(Checksum, object)>> RequestAssetsAsync(
            int serviceId, ISet<Checksum> checksums, CancellationToken cancellationToken)
        {
            var list = new List<(Checksum, object)>();

            foreach (var checksum in checksums)
            {
                object data;
                Assert.True(_map.TryGetValue(checksum, out data));

                list.Add(ValueTuple.Create(checksum, data));
            }

            return Task.FromResult<IList<(Checksum, object)>>(list);
        }
    }
}
