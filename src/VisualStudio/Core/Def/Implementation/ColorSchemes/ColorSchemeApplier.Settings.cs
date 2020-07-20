// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Editor.ColorSchemes;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NativeMethods = Microsoft.CodeAnalysis.Editor.Wpf.Utilities.NativeMethods;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private class ColorSchemeSettings
        {
            private const string ColorSchemeApplierKey = @"Roslyn\ColorSchemeApplier";
            private const string AppliedColorSchemeName = "AppliedColorScheme";

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
                    SchemeName.VisualStudio2019,
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
                    // Flush RegistryKeys out of paranoia
                    itemKey.Flush();
                }

                registryRoot.Flush();

                SetAppliedColorScheme(schemeName);

                // Broadcast that system color settings have changed to force the ColorThemeService to reload colors.
                NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SYSCOLORCHANGE, wparam: IntPtr.Zero, lparam: IntPtr.Zero);
            }

            /// <summary>
            /// Get the color scheme that is applied to the configuration registry.
            /// </summary>
            public SchemeName GetAppliedColorScheme()
            {
                // The applied color scheme is stored in the configuration registry with the color theme information because
                // when the hive gets rebuilt during upgrades, we need to reapply the color scheme information.
                using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: false);
                using var itemKey = registryRoot.OpenSubKey(ColorSchemeApplierKey);
                return itemKey is object
                    ? (SchemeName)itemKey.GetValue(AppliedColorSchemeName)
                    : default;
            }

            private void SetAppliedColorScheme(SchemeName schemeName)
            {
                // The applied color scheme is stored in the configuration registry with the color theme information because
                // when the hive gets rebuilt during upgrades, we need to reapply the color scheme information.
                using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: true);
                using var itemKey = registryRoot.CreateSubKey(ColorSchemeApplierKey);
                itemKey.SetValue(AppliedColorSchemeName, (int)schemeName);
                // Flush RegistryKeys out of paranoia
                itemKey.Flush();
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

                // Since we did not apply 2019 colors if the theme had been customized, default customized themes to 2017 colors.
                var colorScheme = (useEnhancedColorsSetting != ColorSchemeOptions.UseEnhancedColors.DoNotUse && !isThemeCustomized)
                    ? SchemeName.VisualStudio2019
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
                private readonly VisualStudioWorkspace _workspace;

                public HasThemeBeenDefaultedIndexer(VisualStudioWorkspace visualStudioWorkspace)
                    => _workspace = visualStudioWorkspace;

                public bool this[Guid themeId]
                {
                    get => _workspace.Options.GetOption(HasThemeBeenDefaultedOptions.Options[themeId]);

                    set => _workspace.SetOptions(_workspace.Options.WithChangedOption(HasThemeBeenDefaultedOptions.Options[themeId], value));
                }
            }

            internal class HasThemeBeenDefaultedOptions
            {
                internal static readonly ImmutableDictionary<Guid, Option<bool>> Options = new Dictionary<Guid, Option<bool>>
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
            }

            [ExportOptionProvider, Shared]
            internal class HasThemeBeenDefaultedOptionProvider : IOptionProvider
            {
                [ImportingConstructor]
                [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
                public HasThemeBeenDefaultedOptionProvider()
                {
                }

                public ImmutableArray<IOption> Options => HasThemeBeenDefaultedOptions.Options.Values.ToImmutableArray<IOption>();

            }
        }
    }
}
