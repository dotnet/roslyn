// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingRemoteHostOptionsAccessor
    {
        [Obsolete("OOP is now 64bit only.")]
        public static readonly Option2<bool> OOP64Bit = new(
            "InternalFeatureOnOffOptions", nameof(OOP64Bit), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(StorageOptions.LocalRegistryPath + nameof(OOP64Bit)));
    }
}
