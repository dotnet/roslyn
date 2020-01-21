// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingAssetStorageWrappper
    {
        public static UnitTestingAssetStorageWrappper Instance { get; } = new UnitTestingAssetStorageWrappper(AssetStorage.Default);

        internal AssetStorage UnderlyingObject { get; }

        internal UnitTestingAssetStorageWrappper(AssetStorage underlyingObject)
            => UnderlyingObject = underlyingObject ?? throw new ArgumentNullException(nameof(underlyingObject));

        public void UpdateLastActivityTime()
            => UnderlyingObject.UpdateLastActivityTime();
    }
}
