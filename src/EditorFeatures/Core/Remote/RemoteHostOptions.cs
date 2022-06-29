// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteHostOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";
        private const string FeatureName = "InternalFeatureOnOffOptions";

        // use 64bit OOP
        public static readonly Option2<bool> OOP64Bit = new(
            FeatureName, nameof(OOP64Bit), defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OOP64Bit)));

        public static readonly Option2<bool> OOPServerGCFeatureFlag = new(
            FeatureName, nameof(OOPServerGCFeatureFlag), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.OOPServerGC"));

        // use coreclr host for OOP
        public static readonly Option2<bool> OOPCoreClrFeatureFlag = new(
            FeatureName, nameof(OOPCoreClrFeatureFlag), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.ServiceHubCore"));
    }
}
