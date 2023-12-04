// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ColorSchemes;

internal sealed class ColorSchemeOptionsStorage
{
    internal const string ColorSchemeSettingKey = "TextEditor.Roslyn.ColorSchemeName";

    public static readonly Option2<ColorSchemeName> ColorScheme = new(
        "visual_studio_color_scheme_name",
        defaultValue: ColorSchemeName.VisualStudio2019);

    public static readonly Option2<UseEnhancedColors> LegacyUseEnhancedColors = new(
        "visual_studio_color_scheme_use_legacy_enhanced_colors",
        defaultValue: UseEnhancedColors.Default);

    public enum UseEnhancedColors
    {
        Migrated = -2,
        DoNotUse = -1,
        Default = 0,
        Use = 1
    }
}
