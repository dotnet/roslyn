// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    internal static class ServiceFeatureOnOffOptions
    {
        private const bool CSharpClosedFileDiagnosticsEnabledByDefault = false;
        private const bool DefaultClosedFileDiagnosticsEnabledByDefault = true;

        /// <summary>
        /// this option is solely for performance. don't confused by option name. 
        /// this option doesn't mean we will show all diagnostics that belong to opened files when turned off,
        /// rather it means we will only show diagnostics that are cheap to calculate for small scope such as opened files.
        /// </summary>
        public static readonly PerLanguageOption<bool?> ClosedFileDiagnostic = new PerLanguageOption<bool?>(
            "ServiceFeaturesOnOff", "Closed File Diagnostic", defaultValue: null,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Closed File Diagnostic"));

        public static bool IsClosedFileDiagnosticsEnabled(Project project)
        {
            return IsClosedFileDiagnosticsEnabled(project.Solution.Options, project.Language);
        }

        public static bool IsClosedFileDiagnosticsEnabled(OptionSet options, string language)
        {
            var option = options.GetOption(ClosedFileDiagnostic, language);
            if (!option.HasValue)
            {
                return language == LanguageNames.CSharp ?
                    CSharpClosedFileDiagnosticsEnabledByDefault :
                    DefaultClosedFileDiagnosticsEnabledByDefault;
            }

            return option.Value;
        }

        /// <summary>
        /// Option to disable analyzer execution during live analysis.
        /// </summary>
        public static readonly Option<bool> DisableAnalyzers = new Option<bool>(
            nameof(ServiceFeatureOnOffOptions), nameof(DisableAnalyzers), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation($"Options.DisableAnalyzers"));

        /// <summary>
        /// Option to turn off all background analysis to improve performance.
        /// </summary>
        public static readonly Option<bool> PowerSaveMode = new Option<bool>(
            nameof(ServiceFeatureOnOffOptions), nameof(PowerSaveMode), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation($"Options.PowerSaveMode"));

        /// <summary>
        /// Enables forced power save mode when low VM is detected to improve performance.
        /// </summary>
        public static bool LowMemoryForcedPowerSaveMode = false;

        public static bool IsPowerSaveModeEnabled(Project project)
            => IsPowerSaveModeEnabled(project.Solution.Options);

        public static bool IsPowerSaveModeEnabled(OptionSet options)
            => options.GetOption(PowerSaveMode) || LowMemoryForcedPowerSaveMode;

        public static bool IsAnalyzerExecutionDisabled(Project project)
            => IsPowerSaveModeEnabled(project) ||
               project.Solution.Options.GetOption(DisableAnalyzers);
    }
}
