using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Shared.Options
{
    public static class ServiceFeatureOnOffOptions
    {
        /// <summary>
        /// this option is solely for performance. don't confused by option name. 
        /// this option doesn't mean we will show all diagnostics that belong to opened files when turned off,
        /// rather it means we will only show diagnostics that are cheap to calculate for small scope such as opened files.
        /// </summary>
        public static readonly PerLanguageOption<bool?> ClosedFileDiagnostic = Microsoft.CodeAnalysis.Shared.Options.ServiceFeatureOnOffOptions.ClosedFileDiagnostic;
    }
}
