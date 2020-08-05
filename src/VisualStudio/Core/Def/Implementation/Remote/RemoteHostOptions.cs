// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal static class RemoteHostOptions
    {
        // Update primary workspace on OOP every 4 seconds if VS is not running any global operation 
        // such as build, solution open/close, rename and etc.
        // Even if primary workspace is not updated, OOP will work as expected. updating primary workspace 
        // on OOP should let latest data to be synched pre-emptively rather than on demand.
        //
        // 2 second is our usual long running interactive operation delay and 
        // 5 second is usual ambient long running operation delay. 
        // I chose one in between. among 3 and 4 seconds, I chose slower one - 4 second. 
        //
        // When primary workspace is staled, missing data will be synced to OOP on
        // demand and cached for 3 min. enough for primary workspace in OOP to be synced to latest.
        public static readonly Option<int> SolutionChecksumMonitorBackOffTimeSpanInMS = new Option<int>(
            nameof(InternalFeatureOnOffOptions), nameof(SolutionChecksumMonitorBackOffTimeSpanInMS), defaultValue: 4000,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(SolutionChecksumMonitorBackOffTimeSpanInMS)));

        // use 64bit OOP
        public static readonly Option2<bool> OOP64Bit = new Option2<bool>(
            nameof(InternalFeatureOnOffOptions), nameof(OOP64Bit), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(OOP64Bit)));

        public static bool IsServiceHubProcess64Bit(HostWorkspaceServices services)
            => services.GetRequiredService<IOptionService>().GetOption(OOP64Bit);
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
