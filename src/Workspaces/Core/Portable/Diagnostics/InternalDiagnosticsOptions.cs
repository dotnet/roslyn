// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class InternalDiagnosticsOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\Diagnostics\";

        public static readonly Option<bool> BlueSquiggleForBuildDiagnostic = new Option<bool>(nameof(InternalDiagnosticsOptions), "Blue Squiggle For Build Diagnostic", defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Blue Squiggle For Build Diagnostic"));

        public static readonly Option<bool> UseDiagnosticEngineV2 = new Option<bool>(nameof(InternalDiagnosticsOptions), "Use Diagnostic Engine V2", defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Use Diagnostic Engine V2"));

        public static readonly Option<bool> CompilationEndCodeFix = new Option<bool>(nameof(InternalDiagnosticsOptions), "Enable Compilation End Code Fix", defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Enable Compilation End Code Fix"));

        public static readonly Option<bool> UseCompilationEndCodeFixHeuristic = new Option<bool>(nameof(InternalDiagnosticsOptions), "Enable Compilation End Code Fix With Heuristic", defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Enable Compilation End Code Fix With Heuristic"));

        public static readonly Option<bool> BuildErrorIsTheGod = new Option<bool>(nameof(InternalDiagnosticsOptions), "Make build errors to take over everything", defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Make build errors to take over everything"));

        public static readonly Option<bool> ClearLiveErrorsForProjectBuilt = new Option<bool>(nameof(InternalDiagnosticsOptions), "Clear all live errors of projects that got built", defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Clear all live errors of projects that got built"));

        public static readonly Option<bool> PreferLiveErrorsOnOpenedFiles = new Option<bool>(nameof(InternalDiagnosticsOptions), "Live errors will be preferred over errors from build on opened files from same analyzer", defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Live errors will be preferred over errors from build on opened files from same analyzer"));

        public static readonly Option<bool> PreferBuildErrorsOverLiveErrors = new Option<bool>(nameof(InternalDiagnosticsOptions), "Errors from build will be preferred over live errors from same analyzer", defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Errors from build will be preferred over live errors from same analyzer"));

        public static readonly Option<bool> PutCustomTypeInBingSearch = new Option<bool>(nameof(InternalDiagnosticsOptions), nameof(PutCustomTypeInBingSearch), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "PutCustomTypeInBingSearch"));

        public static readonly Option<bool> CrashOnAnalyzerException = new Option<bool>(nameof(InternalDiagnosticsOptions), nameof(CrashOnAnalyzerException), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "CrashOnAnalyzerException"));

        public static readonly Option<bool> ProcessHiddenDiagnostics = new Option<bool>(nameof(InternalDiagnosticsOptions), nameof(ProcessHiddenDiagnostics), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Process Hidden Diagnostics"));
    }
}
