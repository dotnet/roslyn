// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal static class RemoteHostOptions
    {
        [ExportOption]
        public static readonly Option<bool> RemoteHost = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(RemoteHost), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(RemoteHost)));

        [ExportOption]
        public static readonly Option<int> SolutionChecksumMonitorBackOffTimeSpanInMS = new Option<int>(nameof(InternalFeatureOnOffOptions), nameof(SolutionChecksumMonitorBackOffTimeSpanInMS), defaultValue: 10000,
            storageLocations: new LocalUserProfileStorageLocation(InternalFeatureOnOffOptions.LocalRegistryPath + nameof(SolutionChecksumMonitorBackOffTimeSpanInMS)));

        [ExportOption]
        public static readonly Option<bool> RemoteHostTest = new Option<bool>(nameof(InternalFeatureOnOffOptions), nameof(RemoteHostTest), defaultValue: false);
    }
}
