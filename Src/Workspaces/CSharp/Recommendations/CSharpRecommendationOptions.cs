using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations
{
    public static class CSharpRecommendationOptions
    {
        internal const string RecommendationsFeatureName = "CSharp/Recommendations";

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> HideAdvancedMembers = new Option<bool>(RecommendationsFeatureName, "HideAdvancedMembers", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> FilterOutOfScopeLocals = new Option<bool>(RecommendationsFeatureName, "FilterOutOfScopeLocals", defaultValue: true);
    }
}
