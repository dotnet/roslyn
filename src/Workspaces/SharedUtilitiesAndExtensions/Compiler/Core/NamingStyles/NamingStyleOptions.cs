// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;

#if WORKSPACE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.CodeStyle;

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
        isEditorConfigOption: true,
        serializer: EditorConfigValueSerializer<NamingStylePreferences>.Unsupported);

    /// <summary>
    /// When set to <see langword="true"/>, the naming style analyzer will run during command-line builds
    /// and respect individual <c>dotnet_naming_rule.*.severity</c> values. By default, the analyzer is
    /// skipped on build because it has a default severity of <see cref="DiagnosticSeverity.Hidden"/>.
    /// </summary>
    internal static PerLanguageOption2<bool> EnforceNamingStyleInBuild { get; } = new(
        "dotnet_naming_style_enforce_in_build",
        defaultValue: false,
        isEditorConfigOption: true);

    /// <summary>
    /// Options that we expect the user to set in editorconfig.
    /// </summary>
    internal static readonly ImmutableArray<IOption2> EditorConfigOptions = [NamingPreferences, EnforceNamingStyleInBuild];
}

internal interface NamingStylePreferencesProvider
#if WORKSPACE
    : OptionsProvider<NamingStylePreferences>
#endif
{
}
