// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal static class RemoteHostOptions
    {
        [ExportOption]
        public static readonly Option<bool> RemoteHost = new Option<bool>(
            nameof(InternalFeatureOnOffOptions), nameof(RemoteHost), defaultValue: true,
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

        // Default timeout for service hub HubClient.RequestServiceAsync call
        // it turns out HubClient.RequestServiceAsync has internal timeout on how long it will try to connect requested service from service hub
        // if it can't connect, then it throws its own cancellation exception.
        // this is our timeout on how long we will try keep connecting. so far I saw over 2-3 seconds before connection made 
        // when there are many (over 10+ requests) at the same time. one of reasons of this is we put our service hub process as "Below Normal" priority.
        // normally response time is within 10s ms. at most 100ms. if priority is changed to "Normal", most of time 10s ms.
        [ExportOption]
        public static readonly Option<int> RequestServiceTimeoutInMS = new Option<int>(
            nameof(InternalFeatureOnOffOptions), nameof(RequestServiceTimeoutInMS), defaultValue: 7 * 24 * 60 * 60 * 1000 /* 7 day */,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(RequestServiceTimeoutInMS)));

        // This options allow users to restart OOP when it is killed by users
        [ExportOption]
        public static readonly Option<bool> RestartRemoteHostAllowed = new Option<bool>(
            nameof(InternalFeatureOnOffOptions), nameof(RestartRemoteHostAllowed), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(RestartRemoteHostAllowed)));

        [ExportOption]
        public static readonly Option<bool> RemoteHostTest = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(RemoteHostTest), defaultValue: false);
    }
}
