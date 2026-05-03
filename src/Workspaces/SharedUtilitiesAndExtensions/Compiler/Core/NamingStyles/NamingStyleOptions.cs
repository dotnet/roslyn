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
    /// Options that we expect the user to set in editorconfig.
    /// </summary>
    internal static readonly ImmutableArray<IOption2> EditorConfigOptions = [NamingPreferences];
}

internal interface NamingStylePreferencesProvider
#if WORKSPACE
    : OptionsProvider<NamingStylePreferences>
#endif
{
}
