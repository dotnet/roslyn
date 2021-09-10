// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingRemoteHostOptionsAccessor
    {
        public static Option<bool> OOPCoreClr => new(
            RemoteHostOptions.OOPCoreClr.Feature, RemoteHostOptions.OOPCoreClr.Name, defaultValue: RemoteHostOptions.OOPCoreClr.DefaultValue,
            storageLocations: RemoteHostOptions.OOPCoreClr.StorageLocations.ToArray());

        public static Option<bool> OOPCoreClrFeatureFlag => new(
            RemoteHostOptions.OOPCoreClrFeatureFlag.Feature, RemoteHostOptions.OOPCoreClrFeatureFlag.Name, defaultValue: RemoteHostOptions.OOPCoreClrFeatureFlag.DefaultValue,
            storageLocations: RemoteHostOptions.OOPCoreClrFeatureFlag.StorageLocations.ToArray());

        public static bool IsServiceHubProcessCoreClr(IGlobalOptionService globalOptions)
            => RemoteHostOptions.IsServiceHubProcessCoreClr(globalOptions);
    }
}
