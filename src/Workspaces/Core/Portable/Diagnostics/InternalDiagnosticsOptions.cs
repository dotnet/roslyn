// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class InternalDiagnosticsOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\Diagnostics\";

        public static readonly Option2<bool> PreferLiveErrorsOnOpenedFiles = new Option2<bool>(nameof(InternalDiagnosticsOptions), "Live errors will be preferred over errors from build on opened files from same analyzer", defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Live errors will be preferred over errors from build on opened files from same analyzer"));

        public static readonly Option2<bool> PreferBuildErrorsOverLiveErrors = new Option2<bool>(nameof(InternalDiagnosticsOptions), "Errors from build will be preferred over live errors from same analyzer", defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Errors from build will be preferred over live errors from same analyzer"));

        public static readonly Option2<bool> PutCustomTypeInBingSearch = new Option2<bool>(nameof(InternalDiagnosticsOptions), nameof(PutCustomTypeInBingSearch), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "PutCustomTypeInBingSearch"));

        public static readonly Option2<bool> CrashOnAnalyzerException = new Option2<bool>(nameof(InternalDiagnosticsOptions), nameof(CrashOnAnalyzerException), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "CrashOnAnalyzerException"));

        public static readonly Option2<bool> ProcessHiddenDiagnostics = new Option2<bool>(nameof(InternalDiagnosticsOptions), nameof(ProcessHiddenDiagnostics), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Process Hidden Diagnostics"));
    }
}
