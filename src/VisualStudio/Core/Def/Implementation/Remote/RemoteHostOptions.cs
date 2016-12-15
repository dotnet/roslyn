// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal static class RemoteHostOptions
    {
        [ExportOption]
        public static readonly Option<bool> RemoteHost = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(RemoteHost), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(RemoteHost)));

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
        [ExportOption]
        public static readonly Option<int> SolutionChecksumMonitorBackOffTimeSpanInMS = new Option<int>(
            nameof(InternalFeatureOnOffOptions), nameof(SolutionChecksumMonitorBackOffTimeSpanInMS), defaultValue: 4000,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(SolutionChecksumMonitorBackOffTimeSpanInMS)));

        [ExportOption]
        public static readonly Option<bool> RemoteHostTest = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(RemoteHostTest), defaultValue: false);
    }
}
