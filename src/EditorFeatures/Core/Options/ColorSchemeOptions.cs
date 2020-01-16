// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal static class ColorSchemeOptions
    {
        internal const string ColorSchemeSettingKey = "TextEditor.Roslyn.ColorScheme";

        public const string Enhanced = nameof(Enhanced);
        public const string VisualStudio2017 = nameof(VisualStudio2017);

        public static readonly Option<string> ColorScheme = new Option<string>(nameof(ColorSchemeOptions),
            nameof(ColorScheme),
            defaultValue: Enhanced,
            storageLocations: new RoamingProfileStorageLocation(ColorSchemeSettingKey));

        // The applied color scheme is a local setting because it is the scheme that is applied to 
        // the users current registry hive.
        public static readonly Option<string> AppliedColorScheme = new Option<string>(nameof(ColorSchemeOptions),
            nameof(AppliedColorScheme),
            defaultValue: string.Empty,
            storageLocations: new LocalUserProfileStorageLocation(@"Roslyn\ColorSchemeApplier\AppliedColorScheme"));

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

        public ImmutableArray<IOption> Options => ImmutableArray.Create<IOption>(ColorSchemeOptions.ColorScheme);
    }
}
