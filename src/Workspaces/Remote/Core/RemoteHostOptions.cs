// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostOptions
    {
        private const string LocalRegistryPath = StorageOptions.LocalRegistryPath;
        private const string FeatureName = "InternalFeatureOnOffOptions";

        // Update primary workspace on OOP every second if VS is not running any global operation (such as build,
        // solution open/close, rename, etc.)
        //
        // Even if primary workspace is not updated, other OOP queries will work as expected. Updating primary workspace
        // on OOP should let latest data to be synced pre-emptively rather than on demand, and will kick off
        // incremental analyzer tasks.
        public static readonly Option<int> SolutionChecksumMonitorBackOffTimeSpanInMS = new Option<int>(
            FeatureName, nameof(SolutionChecksumMonitorBackOffTimeSpanInMS), defaultValue: 1000,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(SolutionChecksumMonitorBackOffTimeSpanInMS)));

        // use 64bit OOP
        public static readonly Option<bool> OOP64Bit = new Option<bool>(
            FeatureName, nameof(OOP64Bit), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OOP64Bit)));

        public static bool IsServiceHubProcess64Bit(HostWorkspaceServices services)
            => services.GetRequiredService<IOptionService>().GetOption(OOP64Bit) ||
               services.GetRequiredService<IExperimentationService>().IsExperimentEnabled(WellKnownExperimentNames.RoslynOOP64bit);
    }

    [ExportOptionProvider, Shared]
    internal class RemoteHostOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteHostOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS,
            RemoteHostOptions.OOP64Bit);
    }
}
