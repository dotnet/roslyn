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
    [ExportGlobalOptionProvider, Shared]
    internal sealed class RemoteHostOptions : IOptionProvider
    {
        private const string LocalRegistryPath = StorageOptions.LocalRegistryPath;
        private const string FeatureName = "InternalFeatureOnOffOptions";

        // Update primary workspace on OOP every second if VS is not running any global operation (such as build,
        // solution open/close, rename, etc.)
        //
        // Even if primary workspace is not updated, other OOP queries will work as expected. Updating primary workspace
        // on OOP should let latest data to be synced pre-emptively rather than on demand, and will kick off
        // incremental analyzer tasks.
        public static readonly Option2<int> SolutionChecksumMonitorBackOffTimeSpanInMS = new(
            FeatureName, nameof(SolutionChecksumMonitorBackOffTimeSpanInMS), defaultValue: 1000,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(SolutionChecksumMonitorBackOffTimeSpanInMS)));

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

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            SolutionChecksumMonitorBackOffTimeSpanInMS,
            OOP64Bit,
            OOPServerGCFeatureFlag,
            OOPCoreClrFeatureFlag);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteHostOptions()
        {
        }
    }
}
