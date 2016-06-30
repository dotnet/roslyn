// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class InternalDiagnosticsOptions
    {
        public const string OptionName = "InternalDiagnosticsOptions";
        private const string LocalRegistryPath = @"Roslyn\Internal\Diagnostics\";

        public static readonly Option<bool> BlueSquiggleForBuildDiagnostic = new Option<bool>(OptionName, "Blue Squiggle For Build Diagnostic", defaultValue: false,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Blue Squiggle For Build Diagnostic"));

        public static readonly Option<bool> UseDiagnosticEngineV2 = new Option<bool>(OptionName, "Use Diagnostic Engine V2", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Use Diagnostic Engine V2"));

        public static readonly Option<bool> CompilationEndCodeFix = new Option<bool>(OptionName, "Enable Compilation End Code Fix", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Enable Compilation End Code Fix"));

        public static readonly Option<bool> UseCompilationEndCodeFixHeuristic = new Option<bool>(OptionName, "Enable Compilation End Code Fix With Heuristic", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Enable Compilation End Code Fix With Heuristic"));

        public static readonly Option<bool> BuildErrorIsTheGod = new Option<bool>(OptionName, "Make build errors to take over everything", defaultValue: false,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Make build errors to take over everything"));

        public static readonly Option<bool> ClearLiveErrorsForProjectBuilt = new Option<bool>(OptionName, "Clear all live errors of projects that got built", defaultValue: false,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Clear all live errors of projects that got built"));

        public static readonly Option<bool> PreferLiveErrorsOnOpenedFiles = new Option<bool>(OptionName, "Live errors will be preferred over errors from build on opened files from same analyzer", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Live errors will be preferred over errors from build on opened files from same analyzer"));

        public static readonly Option<bool> PreferBuildErrorsOverLiveErrors = new Option<bool>(OptionName, "Errors from build will be preferred over live errors from same analyzer", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Errors from build will be preferred over live errors from same analyzer"));

        public static readonly Option<bool> PutCustomTypeInBingSearch = new Option<bool>(OptionName, "PutCustomTypeInBingSearch", defaultValue: true,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "PutCustomTypeInBingSearch"));

        public static readonly Option<bool> CrashOnAnalyzerException = new Option<bool>(OptionName, "CrashOnAnalyzerException", defaultValue: false,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "CrashOnAnalyzerException"));

        public static readonly Option<bool> ProcessHiddenDiagnostics = new Option<bool>(OptionName, "Process Hidden Diagnostics", defaultValue: false,
            persistences: new LocalUserProfilePersistence(LocalRegistryPath + "Process Hidden Diagnostics"));
    }
}
