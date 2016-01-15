// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class InternalDiagnosticsOptions
    {
        public const string OptionName = "InternalDiagnosticsOptions";

        public static readonly Option<bool> BlueSquiggleForBuildDiagnostic = new Option<bool>(OptionName, "Blue Squiggle For Build Diagnostic", defaultValue: false);
        public static readonly Option<bool> UseDiagnosticEngineV2 = new Option<bool>(OptionName, "Use Diagnostic Engine V2", defaultValue: false);
        public static readonly Option<bool> CompilationEndCodeFix = new Option<bool>(OptionName, "Enable Compilation End Code Fix", defaultValue: true);
        public static readonly Option<bool> UseCompilationEndCodeFixHeuristic = new Option<bool>(OptionName, "Enable Compilation End Code Fix With Heuristic", defaultValue: true);
        public static readonly Option<bool> BuildErrorIsTheGod = new Option<bool>(OptionName, "Make build errors to take over everything", defaultValue: false);
        public static readonly Option<bool> ClearLiveErrorsForProjectBuilt = new Option<bool>(OptionName, "Clear all live errors of projects that got built", defaultValue: false);
        public static readonly Option<bool> PreferLiveErrorsOnOpenedFiles = new Option<bool>(OptionName, "Live errors will be preferred over errors from build on opened files from same analyzer", defaultValue: true);
        public static readonly Option<bool> PreferBuildErrorsOverLiveErrors = new Option<bool>(OptionName, "Errors from build will be preferred over live errors from same analyzer", defaultValue: true);
        public static readonly Option<bool> PutCustomTypeInBingSearch = new Option<bool>(OptionName, "PutCustomTypeInBingSearch", defaultValue: true);
        public static readonly Option<bool> CrashOnAnalyzerException = new Option<bool>(OptionName, "CrashOnAnalyzerException", defaultValue: false);
    }
}
