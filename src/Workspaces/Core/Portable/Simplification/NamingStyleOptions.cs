// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

#if CODE_STYLE
namespace Microsoft.CodeAnalysis.Internal.Options
#else
using Microsoft.CodeAnalysis.Options;
namespace Microsoft.CodeAnalysis.Simplification
#endif
{
#if CODE_STYLE
    public static class NamingStyleOptions
#else
    internal static class NamingStyleOptions
#endif
    {
        // Use 'SimplificationOptions' for back compat as the below option 'NamingPreferences' was defined with feature name 'SimplificationOptions'.
        private const string FeatureName = "SimplificationOptions";

        /// <summary>
        /// This option describes the naming rules that should be applied to specified categories of symbols, 
        /// and the level to which those rules should be enforced.
        /// </summary>
        internal static PerLanguageOption<NamingStylePreferences> NamingPreferences { get; } = new PerLanguageOption<NamingStylePreferences>(FeatureName, nameof(NamingPreferences), defaultValue: NamingStylePreferences.Default,
            storageLocations: new OptionStorageLocation[] {
                new NamingStylePreferenceEditorConfigStorageLocation(),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.NamingPreferences5"),
                new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.NamingPreferences")
            });

        public static OptionKey GetNamingPreferencesOptionKey(string language)
            => new OptionKey(NamingPreferences, language);
    }
}
