// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal static class NamingStyleOptions
    {
        // Use 'SimplificationOptions' for back compat as the below option 'NamingPreferences' was defined with feature name 'SimplificationOptions'.
        private const string FeatureName = "SimplificationOptions";

        /// <summary>
        /// This option describes the naming rules that should be applied to specified categories of symbols, 
        /// and the level to which those rules should be enforced.
        /// </summary>
        internal static PerLanguageOption2<NamingStylePreferences> NamingPreferences { get; } = new PerLanguageOption2<NamingStylePreferences>(
            FeatureName, nameof(NamingPreferences), defaultValue: NamingStylePreferences.Default,
            new NamingStylePreferenceEditorConfigStorageLocation(),
            new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.NamingPreferences5"),
            new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.NamingPreferences"));

        public static OptionKey2 GetNamingPreferencesOptionKey(string language)
            => new(NamingPreferences, language);
    }

#if !CODE_STYLE
    internal delegate NamingStylePreferences NamingStylePreferencesProvider(HostLanguageServices languageServices);
#endif
}
