// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.ColorSchemes;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal static class ColorSchemeOptions
    {
        internal const string ColorSchemeSettingKey = "TextEditor.Roslyn.ColorScheme";

        public static readonly Option<SchemeName> ColorScheme = new Option<SchemeName>(nameof(ColorSchemeOptions),
            nameof(ColorScheme),
            defaultValue: SchemeName.VisualStudio2019,
            storageLocations: new RoamingProfileStorageLocation(ColorSchemeSettingKey));

        public static readonly Option<UseEnhancedColors> LegacyUseEnhancedColors = new Option<UseEnhancedColors>(nameof(ColorSchemeOptions),
            nameof(LegacyUseEnhancedColors),
            defaultValue: UseEnhancedColors.Default,
            storageLocations: new RoamingProfileStorageLocation("WindowManagement.Options.UseEnhancedColorsForManagedLanguages"));

        public enum UseEnhancedColors
        {
            Migrated = -2,
            DoNotUse = -1,
            Default = 0,
            Use = 1
        }
    }

    [ExportOptionProvider, Shared]
    internal class ColorSchemeOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public ColorSchemeOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options => ImmutableArray.Create<IOption>(
            ColorSchemeOptions.ColorScheme,
            ColorSchemeOptions.LegacyUseEnhancedColors);
    }
}
