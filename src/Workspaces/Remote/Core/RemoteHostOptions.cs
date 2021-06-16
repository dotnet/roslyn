// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.InteropServices;
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

        // use Server GC for 64-bit OOP
        public static readonly Option2<bool> OOPServerGC = new Option2<bool>(
            FeatureName, nameof(OOPServerGC), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OOPServerGC)));

        // use coreclr host for OOP
        public static readonly Option2<bool> OOPCoreClr = new Option2<bool>(
            FeatureName, nameof(OOPCoreClr), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OOPCoreClr)));

        public static bool IsServiceHubProcessServerGC(HostWorkspaceServices services)
        {
            return services.GetRequiredService<IOptionService>().GetOption(OOPServerGC)
                || services.GetService<IExperimentationService>()?.IsExperimentEnabled(WellKnownExperimentNames.OOPServerGC) == true;
        }

        public static bool IsServiceHubProcessCoreClr(HostWorkspaceServices services)
        {
            return services.GetRequiredService<IOptionService>().GetOption(OOPCoreClr)
                || services.GetService<IExperimentationService>()?.IsExperimentEnabled(WellKnownExperimentNames.OOPCoreClr) == true;
        }

        public static bool IsCurrentProcessRunningOnCoreClr()
        {
            return !RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework")
                && !RuntimeInformation.FrameworkDescription.StartsWith(".NET Native");
        }
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
            RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS);
    }
}
