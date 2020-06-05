// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [Obsolete]
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
