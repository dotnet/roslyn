using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    internal static class ServiceFeatureOnOffOptions
    {
        /// <summary>
        /// This option is used by TypeScript.
        /// </summary>
        [Obsolete("Use the new option SolutionCrawlerOptions.BackgroundAnalysisScopeOption")]
        public static readonly PerLanguageOption<bool?> ClosedFileDiagnostic = new PerLanguageOption<bool?>(
            "ServiceFeaturesOnOff", "Closed File Diagnostic", defaultValue: null,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Closed File Diagnostic"));
    }
}
