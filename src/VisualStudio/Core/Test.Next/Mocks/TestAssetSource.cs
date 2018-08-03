// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Shared;

namespace Roslyn.VisualStudio.Next.UnitTests.Mocks
{
    internal class TestAssetSource : SimpleAssetSource
    {
        public TestAssetSource(AssetStorage assetStorage) :
            base(assetStorage, new Dictionary<Checksum, object>())
        {
        }

        public TestAssetSource(AssetStorage assetStorage, Checksum checksum, object data) :
            base(assetStorage, new Dictionary<Checksum, object>() { { checksum, data } })
        {
        }
    }
}
