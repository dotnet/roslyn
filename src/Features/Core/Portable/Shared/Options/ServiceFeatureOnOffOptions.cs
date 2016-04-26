// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    internal static class ServiceFeatureOnOffOptions
    {
        public const string OptionName = "ServiceFeaturesOnOff";

        private const bool CSharpClosedFileDiagnosticsEnabledByDefault = false;
        private const bool DefaultClosedFileDiagnosticsEnabledByDefault = true;

        /// <summary>
        /// this option is solely for performance. don't confused by option name. 
        /// this option doesn't mean we will show all diagnostics that belong to opened files when turned off,
        /// rather it means we will only show diagnostics that are cheap to calculate for small scope such as opened files.
        /// </summary>
        public static readonly PerLanguageOption<bool?> ClosedFileDiagnostic = new PerLanguageOption<bool?>(OptionName, "Closed File Diagnostic", defaultValue: null);

        public static bool IsClosedFileDiagnosticsEnabled(Workspace workspace, string language)
        {
            var optionsService = workspace.Services.GetService<IOptionService>();
            return optionsService != null && IsClosedFileDiagnosticsEnabled(optionsService, language);
        }

        public static bool IsClosedFileDiagnosticsEnabled(IOptionService optionService, string language)
        {
            var option = optionService.GetOption(ClosedFileDiagnostic, language);
            if (!option.HasValue)
            {
                return language == LanguageNames.CSharp ?
                    CSharpClosedFileDiagnosticsEnabledByDefault :
                    DefaultClosedFileDiagnosticsEnabledByDefault;
            }

            return option.Value;
        }
    }
}
