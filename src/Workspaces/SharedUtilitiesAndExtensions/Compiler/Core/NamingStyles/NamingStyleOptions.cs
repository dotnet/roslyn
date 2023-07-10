// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class NamingStyleOptions
    {
        public const string NamingPreferencesOptionName = "dotnet_naming_preferences";

        /// <summary>
        /// This option describes the naming rules that should be applied to specified categories of symbols, 
        /// and the level to which those rules should be enforced.
        /// </summary>
        internal static PerLanguageOption2<NamingStylePreferences> NamingPreferences { get; } = new(
            NamingPreferencesOptionName,
            defaultValue: NamingStylePreferences.Default,
            isEditorConfigOption: true);
    }

    internal interface NamingStylePreferencesProvider
#if !CODE_STYLE
        : OptionsProvider<NamingStylePreferences>
#endif
    {
    }

#if !CODE_STYLE
    internal static class NamingStylePreferencesProviders
    {
        public static async ValueTask<NamingStylePreferences> GetNamingStylePreferencesAsync(this Document document, NamingStylePreferences? fallbackOptions, CancellationToken cancellationToken)
        {
            var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            return configOptions.GetEditorConfigOption(NamingStyleOptions.NamingPreferences, fallbackOptions ?? NamingStylePreferences.Default);
        }

        public static async ValueTask<NamingStylePreferences> GetNamingStylePreferencesAsync(this Document document, NamingStylePreferencesProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        {
            var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            if (configOptions.TryGetEditorConfigOption<NamingStylePreferences>(NamingStyleOptions.NamingPreferences, out var value))
            {
                return value;
            }

            return await fallbackOptionsProvider.GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false);
        }
    }
#endif
}
