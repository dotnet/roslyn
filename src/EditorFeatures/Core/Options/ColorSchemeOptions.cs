// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal static class ColorSchemeOptions
    {
        public const string SettingKey = "TextEditor.Roslyn.ColorScheme";

        public const string Enhanced = nameof(Enhanced);
        public const string VisualStudio2017 = nameof(VisualStudio2017);

        public static readonly Option<string> ColorScheme = new Option<string>(nameof(ColorSchemeOptions),
            nameof(ColorScheme),
            defaultValue: Enhanced,
            storageLocations: new OptionStorageLocation[]
            {
                new RoamingProfileStorageLocation(SettingKey)
            });
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
