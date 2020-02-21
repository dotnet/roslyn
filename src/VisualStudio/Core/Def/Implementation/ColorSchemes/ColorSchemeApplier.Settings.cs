// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
            private readonly IGlobalOptionService _optionService;

            public HasThemeBeenDefaultedIndexer HasThemeBeenDefaulted { get; }

            public ColorSchemeSettings(IServiceProvider serviceProvider, IGlobalOptionService globalOptionService)
            {
                _serviceProvider = serviceProvider;
                _optionService = globalOptionService;

                HasThemeBeenDefaulted = new HasThemeBeenDefaultedIndexer(globalOptionService);
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

                SetOption(_optionService, ColorSchemeOptions.AppliedColorScheme, schemeName);

                // Broadcast that system color settings have changed to force the ColorThemeService to reload colors.
                NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SYSCOLORCHANGE, wparam: IntPtr.Zero, lparam: IntPtr.Zero);
            }

            public SchemeName GetAppliedColorScheme()
            {
                var schemeName = _optionService.GetOption(ColorSchemeOptions.AppliedColorScheme);
                return schemeName != SchemeName.None
                    ? schemeName
                    : ColorSchemeOptions.AppliedColorScheme.DefaultValue;
            }

            public SchemeName GetConfiguredColorScheme()
            {
                var schemeName = _optionService.GetOption(ColorSchemeOptions.ColorScheme);
                return schemeName != SchemeName.None
                    ? schemeName
                    : ColorSchemeOptions.ColorScheme.DefaultValue;
            }

            public void MigrateToColorSchemeSetting(bool isThemeCustomized)
            {
                // Get the preview feature flag value.
                var useEnhancedColorsSetting = _optionService.GetOption(ColorSchemeOptions.LegacyUseEnhancedColors);

                // Return if we have already migrated.
                if (useEnhancedColorsSetting == ColorSchemeOptions.UseEnhancedColors.Migrated)
                {
                    return;
                }

                // Since we did not apply enhanced colors if the theme had been customized, default customized themes to classic colors.
                var colorScheme = (useEnhancedColorsSetting != ColorSchemeOptions.UseEnhancedColors.DoNotUse && !isThemeCustomized)
                    ? SchemeName.Enhanced
                    : SchemeName.VisualStudio2017;

                SetOption(_optionService, ColorSchemeOptions.ColorScheme, colorScheme);
                SetOption(_optionService, ColorSchemeOptions.LegacyUseEnhancedColors, ColorSchemeOptions.UseEnhancedColors.Migrated);
            }

            public Guid GetThemeId()
            {
                // Look up the value from the new roamed theme property first and
                // fallback to the original roamed theme property if that fails.
                var themeIdString = _optionService.GetOption(VisualStudioColorTheme.CurrentThemeNew)
                    ?? _optionService.GetOption(VisualStudioColorTheme.CurrentTheme);

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

                private readonly IGlobalOptionService _optionService;

                public HasThemeBeenDefaultedIndexer(IGlobalOptionService globalOptionService)
                {
                    _optionService = globalOptionService;
                }

                public bool this[Guid themeId]
                {
                    get => _optionService.GetOption(HasThemeBeenDefaultedOptions[themeId]);

                    set => SetOption(_optionService, HasThemeBeenDefaultedOptions[themeId], value);
                }
            }

            private static readonly EmptyOptionService s_emptyOptionService = new EmptyOptionService();
            private static readonly ImmutableHashSet<string> s_emptyStringSet = ImmutableHashSet.Create(string.Empty);
            private static void SetOption(IGlobalOptionService optionService, IOption option, object? value)
            {
                var optionKey = new OptionKey(option);
                var optionSet = optionService.GetSerializableOptionsSnapshot(s_emptyStringSet, s_emptyOptionService);
                optionService.SetOptions(optionSet.WithChangedOption(optionKey, value));
            }

            private sealed class EmptyOptionService : IOptionService
            {
                public event EventHandler<OptionChangedEventArgs>? OptionChanged;

                [return: MaybeNull]
                public T GetOption<T>(Option<T> option)
                {
                    throw new NotImplementedException();
                }

                [return: MaybeNull]
                public T GetOption<T>(PerLanguageOption<T> option, string? languageName)
                {
                    throw new NotImplementedException();
                }

                public object? GetOption(OptionKey optionKey)
                {
                    return null;
                }

                public SerializableOptionSet GetOptions()
                {
                    throw new NotImplementedException();
                }

                public IEnumerable<IOption> GetRegisteredOptions()
                {
                    throw new NotImplementedException();
                }

                public ImmutableHashSet<IOption> GetRegisteredSerializableOptions(ImmutableHashSet<string> languages)
                {
                    throw new NotImplementedException();
                }

                public SerializableOptionSet GetSerializableOptionsSnapshot(ImmutableHashSet<string> languages)
                {
                    throw new NotImplementedException();
                }

                public Task<OptionSet> GetUpdatedOptionSetForDocumentAsync(Document document, OptionSet optionSet, CancellationToken cancellationToken)
                {
                    throw new NotImplementedException();
                }

                public void RegisterDocumentOptionsProvider(IDocumentOptionsProvider documentOptionsProvider)
                {
                    throw new NotImplementedException();
                }

                public void RegisterWorkspace(Workspace workspace)
                {
                    throw new NotImplementedException();
                }

                public void SetOptions(OptionSet optionSet)
                {
                    throw new NotImplementedException();
                }

                public void UnregisterWorkspace(Workspace workspace)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
