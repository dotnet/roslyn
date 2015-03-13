// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class InternalDiagnosticsOptions
    {
        public const string OptionName = "InternalDiagnosticsOptions";

        [ExportOption]
        public static readonly Option<bool> BlueSquiggleForBuildDiagnostic = new Option<bool>(OptionName, "Blue Squiggle For Build Diagnostic", defaultValue: false);

        [ExportOption]
        public static readonly Option<bool> UseDiagnosticEngineV2 = new Option<bool>(OptionName, "Use Diagnostic Engine V2", defaultValue: false);

        [ExportOption]
        public static readonly Option<bool> CompilationEndCodeFix = new Option<bool>(OptionName, "Enable Compilation End Code Fix", defaultValue: true);

        [ExportOption]
        public static readonly Option<bool> UseCompilationEndCodeFixHeuristic = new Option<bool>(OptionName, "Enable Compilation End Code Fix With Heuristic", defaultValue: true);
    }
}
