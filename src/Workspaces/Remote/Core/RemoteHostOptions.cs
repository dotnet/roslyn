// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
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
        public static readonly Option2<bool> OOP64Bit = new Option2<bool>(
            FeatureName, nameof(OOP64Bit), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OOP64Bit)));

        // Override 64-bit OOP option to force use of a 32-bit process. This option exists as a registry-based
        // workaround for cases where the new 64-bit mode fails and 32-bit in-process fails to provide a viable
        // fallback.
        public static readonly Option2<bool> OOP32BitOverride = new Option2<bool>(
            FeatureName, nameof(OOP32BitOverride), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OOP32BitOverride)));

        public static bool IsServiceHubProcess64Bit(HostWorkspaceServices services)
            => IsUsingServiceHubOutOfProcess(services) && !services.GetRequiredService<IOptionService>().GetOption(OOP32BitOverride);

        /// <summary>
        /// Determines whether ServiceHub out-of-process execution is enabled for Roslyn.
        /// </summary>
        public static bool IsUsingServiceHubOutOfProcess(HostWorkspaceServices services)
        {
            var optionService = services.GetRequiredService<IOptionService>();
            if (Environment.Is64BitOperatingSystem && optionService.GetOption(OOP64Bit))
            {
                // OOP64Bit is set and supported
                return true;
            }

            if (optionService.GetOption(OOP32BitOverride))
            {
                // Hidden fallback to 32-bit OOP is set
                return true;
            }

            return false;
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
            RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS,
            RemoteHostOptions.OOP64Bit);
    }
}
