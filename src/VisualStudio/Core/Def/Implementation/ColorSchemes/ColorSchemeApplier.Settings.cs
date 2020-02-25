// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Editor.ColorSchemes;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NativeMethods = Microsoft.CodeAnalysis.Editor.Wpf.Utilities.NativeMethods;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private class ColorSchemeSettings
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly VisualStudioWorkspace _workspace;

            public HasThemeBeenDefaultedIndexer HasThemeBeenDefaulted { get; }

            public ColorSchemeSettings(IServiceProvider serviceProvider, VisualStudioWorkspace visualStudioWorkspace)
            {
                _serviceProvider = serviceProvider;
                _workspace = visualStudioWorkspace;

                HasThemeBeenDefaulted = new HasThemeBeenDefaultedIndexer(visualStudioWorkspace);
            }

            public ImmutableDictionary<SchemeName, ColorScheme> GetColorSchemes()
            {
                return new[]
                {
                    SchemeName.Enhanced,
                    SchemeName.VisualStudio2017
                }.ToImmutableDictionary(name => name, name => GetColorScheme(name));
            }

            private ColorScheme GetColorScheme(SchemeName schemeName)
            {
                using var colorSchemeStream = GetColorSchemeXmlStream(schemeName);
                return ColorSchemeReader.ReadColorScheme(colorSchemeStream);
            }

            private Stream GetColorSchemeXmlStream(SchemeName schemeName)
            {
                var assembly = Assembly.GetExecutingAssembly();
                return assembly.GetManifestResourceStream($"Microsoft.VisualStudio.LanguageServices.ColorSchemes.{schemeName}.xml");
            }

            public void ApplyColorScheme(SchemeName schemeName, ImmutableArray<RegistryItem> registryItems)
            {
                using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: true);

                foreach (var item in registryItems)
                {
                    using var itemKey = registryRoot.CreateSubKey(item.SectionName);
                    itemKey.SetValue(item.ValueName, item.ValueData);
                }

                _workspace.SetOptions(_workspace.Options.WithChangedOption(ColorSchemeOptions.AppliedColorScheme, schemeName));

                // Broadcast that system color settings have changed to force the ColorThemeService to reload colors.
                NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SYSCOLORCHANGE, wparam: IntPtr.Zero, lparam: IntPtr.Zero);
            }

            public SchemeName GetAppliedColorScheme()
            {
                var schemeName = _workspace.Options.GetOption(ColorSchemeOptions.AppliedColorScheme);
                return schemeName != SchemeName.None
                    ? schemeName
                    : ColorSchemeOptions.AppliedColorScheme.DefaultValue;
            }

            public SchemeName GetConfiguredColorScheme()
            {
                var schemeName = _workspace.Options.GetOption(ColorSchemeOptions.ColorScheme);
                return schemeName != SchemeName.None
                    ? schemeName
                    : ColorSchemeOptions.ColorScheme.DefaultValue;
            }

            public void MigrateToColorSchemeSetting(bool isThemeCustomized)
            {
                // Get the preview feature flag value.
                var useEnhancedColorsSetting = _workspace.Options.GetOption(ColorSchemeOptions.LegacyUseEnhancedColors);

                // Return if we have already migrated.
                if (useEnhancedColorsSetting == ColorSchemeOptions.UseEnhancedColors.Migrated)
                {
                    return;
                }

                // Since we did not apply enhanced colors if the theme had been customized, default customized themes to classic colors.
                var colorScheme = (useEnhancedColorsSetting != ColorSchemeOptions.UseEnhancedColors.DoNotUse && !isThemeCustomized)
                    ? SchemeName.Enhanced
                    : SchemeName.VisualStudio2017;

                _workspace.SetOptions(_workspace.Options.WithChangedOption(ColorSchemeOptions.ColorScheme, colorScheme));
                _workspace.SetOptions(_workspace.Options.WithChangedOption(ColorSchemeOptions.LegacyUseEnhancedColors, ColorSchemeOptions.UseEnhancedColors.Migrated));
            }

            public Guid GetThemeId()
            {
                // Look up the value from the new roamed theme property first and
                // fallback to the original roamed theme property if that fails.
                var themeIdString = _workspace.Options.GetOption(VisualStudioColorTheme.CurrentThemeNew)
                    ?? _workspace.Options.GetOption(VisualStudioColorTheme.CurrentTheme);

                return Guid.TryParse(themeIdString, out var themeId) ? themeId : Guid.Empty;
            }

            private static class VisualStudioColorTheme
            {
                private const string CurrentThemeValueName = "Microsoft.VisualStudio.ColorTheme";
                private const string CurrentThemeValueNameNew = "Microsoft.VisualStudio.ColorThemeNew";

                public static readonly Option<string?> CurrentTheme = new Option<string?>(nameof(VisualStudioColorTheme),
                    nameof(CurrentTheme),
                    defaultValue: null,
                    storageLocations: new RoamingProfileStorageLocation(CurrentThemeValueName));

                public static readonly Option<string?> CurrentThemeNew = new Option<string?>(nameof(VisualStudioColorTheme),
                    nameof(CurrentThemeNew),
                    defaultValue: null,
                    storageLocations: new RoamingProfileStorageLocation(CurrentThemeValueNameNew));
            }

            public sealed class HasThemeBeenDefaultedIndexer
            {
                private static readonly ImmutableDictionary<Guid, Option<bool>> HasThemeBeenDefaultedOptions = new Dictionary<Guid, Option<bool>>
                {
                    [KnownColorThemes.Blue] = CreateHasThemeBeenDefaultedOption(KnownColorThemes.Blue),
                    [KnownColorThemes.Light] = CreateHasThemeBeenDefaultedOption(KnownColorThemes.Light),
                    [KnownColorThemes.Dark] = CreateHasThemeBeenDefaultedOption(KnownColorThemes.Dark),
                    [KnownColorThemes.AdditionalContrast] = CreateHasThemeBeenDefaultedOption(KnownColorThemes.AdditionalContrast)
                }.ToImmutableDictionary();

                private static Option<bool> CreateHasThemeBeenDefaultedOption(Guid themeId)
                {
                    return new Option<bool>(nameof(ColorSchemeApplier), $"{nameof(HasThemeBeenDefaultedOptions)}{themeId}", defaultValue: false,
                        storageLocations: new RoamingProfileStorageLocation($@"Roslyn\ColorSchemeApplier\HasThemeBeenDefaulted\{themeId}"));
                }

                private readonly VisualStudioWorkspace _workspace;

                public HasThemeBeenDefaultedIndexer(VisualStudioWorkspace visualStudioWorkspace)
                {
                    _workspace = visualStudioWorkspace;
                }

                public bool this[Guid themeId]
                {
                    get => _workspace.Options.GetOption(HasThemeBeenDefaultedOptions[themeId]);

                    set => _workspace.SetOptions(_workspace.Options.WithChangedOption(HasThemeBeenDefaultedOptions[themeId], value));
                }
            }
        }
    }
}
