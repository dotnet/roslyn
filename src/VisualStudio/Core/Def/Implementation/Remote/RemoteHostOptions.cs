// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal static class RemoteHostOptions
    {
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
        public static readonly Option<int> SolutionChecksumMonitorBackOffTimeSpanInMS = new Option<int>(
            nameof(InternalFeatureOnOffOptions), nameof(SolutionChecksumMonitorBackOffTimeSpanInMS), defaultValue: 4000,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(SolutionChecksumMonitorBackOffTimeSpanInMS)));

        // Default timeout for service hub HubClient.RequestServiceAsync call
        // it turns out HubClient.RequestServiceAsync has internal timeout on how long it will try to connect requested service from service hub
        // if it can't connect, then it throws its own cancellation exception.
        // this is our timeout on how long we will try keep connecting. so far I saw over 2-3 seconds before connection made 
        // when there are many (over 10+ requests) at the same time. one of reasons of this is we put our service hub process as "Below Normal" priority.
        // normally response time is within 10s ms. at most 100ms. if priority is changed to "Normal", most of time 10s ms.
        // 
        // also another reason why timeout is so big is that, if user put this computer sleep, then timeout can happen. so for now, until we have
        // sleep aware timer, we put very long timeout for request service (https://github.com/dotnet/roslyn/pull/22151)
        public static readonly Option<int> RequestServiceTimeoutInMS = new Option<int>(
            nameof(InternalFeatureOnOffOptions), nameof(RequestServiceTimeoutInMS), defaultValue: 7 * 24 * 60 * 60 * 1000 /* 7 days */,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(RequestServiceTimeoutInMS)));

        // This options allow users to restart OOP when it is killed by users
        public static readonly Option<bool> RestartRemoteHostAllowed = new Option<bool>(
            nameof(InternalFeatureOnOffOptions), nameof(RestartRemoteHostAllowed), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(RestartRemoteHostAllowed)));

        // use 64bit OOP
        public static readonly Option<bool> OOP64Bit = new Option<bool>(
            nameof(InternalFeatureOnOffOptions), nameof(OOP64Bit), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(OOP64Bit)));

        public static readonly Option<bool> RemoteHostTest = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(RemoteHostTest), defaultValue: false);

        public static readonly Option<bool> EnableConnectionPool = new Option<bool>(
            nameof(InternalFeatureOnOffOptions), nameof(EnableConnectionPool), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(EnableConnectionPool)));

        /// <summary>
        /// default 15 is chosen which is big enough but not too big for service hub to handle
        /// </summary>
        public static readonly Option<int> MaxPoolConnection = new Option<int>(
            nameof(InternalFeatureOnOffOptions), nameof(MaxPoolConnection), defaultValue: 15,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(MaxPoolConnection)));
    }

    [ExportOptionProvider, Shared]
    internal class RemoteHostOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public RemoteHostOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            RemoteHostOptions.RemoteHost,
            RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS,
            RemoteHostOptions.RequestServiceTimeoutInMS,
            RemoteHostOptions.RestartRemoteHostAllowed,
            RemoteHostOptions.OOP64Bit,
            RemoteHostOptions.RemoteHostTest,
            RemoteHostOptions.EnableConnectionPool,
            RemoteHostOptions.MaxPoolConnection);
    }
}
