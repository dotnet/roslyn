// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using NativeMethods = Microsoft.CodeAnalysis.Editor.Wpf.Utilities.NativeMethods;

namespace Microsoft.CodeAnalysis.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private class ColorSchemeSettings
        {
            private const string ColorSchemeApplierKey = @"Roslyn\ColorSchemeApplier";
            private const string AppliedColorSchemeName = "AppliedColorScheme";

            private readonly IThreadingContext _threadingContext;
            private readonly IServiceProvider _serviceProvider;
            private readonly IGlobalOptionService _globalOptions;

            public ColorSchemeSettings(
                IThreadingContext threadingContext,
                IServiceProvider serviceProvider,
                IGlobalOptionService globalOptions)
            {
                _threadingContext = threadingContext;
                _serviceProvider = serviceProvider;
                _globalOptions = globalOptions;
            }

            public static ImmutableDictionary<ColorSchemeName, ColorScheme> GetColorSchemes()
            {
                return new[]
                {
                    ColorSchemeName.VisualStudio2019,
                    ColorSchemeName.VisualStudio2017
                }.ToImmutableDictionary(name => name, GetColorScheme);
            }

            private static ColorScheme GetColorScheme(ColorSchemeName schemeName)
            {
                using var colorSchemeStream = GetColorSchemeXmlStream(schemeName);
                return ColorSchemeReader.ReadColorScheme(colorSchemeStream);
            }

            private static Stream GetColorSchemeXmlStream(ColorSchemeName schemeName)
            {
                var assembly = Assembly.GetExecutingAssembly();
                return assembly.GetManifestResourceStream($"Microsoft.VisualStudio.LanguageServices.ColorSchemes.{schemeName}.xml");
            }

            public async Task ApplyColorSchemeAsync(
                ColorSchemeName schemeName, ImmutableArray<RegistryItem> registryItems, CancellationToken cancellationToken)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: true);

                foreach (var item in registryItems)
                {
                    using var itemKey = registryRoot.CreateSubKey(item.SectionName);
                    itemKey.SetValue(RegistryItem.ValueName, item.ValueData);
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
            public async Task<ColorSchemeName> GetAppliedColorSchemeAsync(CancellationToken cancellationToken)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                // The applied color scheme is stored in the configuration registry with the color theme information because
                // when the hive gets rebuilt during upgrades, we need to reapply the color scheme information.
                using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: false);
                using var itemKey = registryRoot.OpenSubKey(ColorSchemeApplierKey);
                return itemKey is object
                    ? (ColorSchemeName)itemKey.GetValue(AppliedColorSchemeName)
                    : default;
            }

            private void SetAppliedColorScheme(ColorSchemeName schemeName)
            {
                _threadingContext.ThrowIfNotOnUIThread();

                // The applied color scheme is stored in the configuration registry with the color theme information because
                // when the hive gets rebuilt during upgrades, we need to reapply the color scheme information.
                using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: true);
                using var itemKey = registryRoot.CreateSubKey(ColorSchemeApplierKey);
                itemKey.SetValue(AppliedColorSchemeName, (int)schemeName);
                // Flush RegistryKeys out of paranoia
                itemKey.Flush();
            }

            public ColorSchemeName GetConfiguredColorScheme()
            {
                var schemeName = _globalOptions.GetOption(ColorSchemeOptions.ColorScheme);
                return schemeName != ColorSchemeName.None
                    ? schemeName
                    : ColorSchemeOptions.ColorScheme.DefaultValue;
            }

            public void MigrateToColorSchemeSetting()
            {
                // Get the preview feature flag value.
                var useEnhancedColorsSetting = _globalOptions.GetOption(ColorSchemeOptions.LegacyUseEnhancedColors);

                // Return if we have already migrated.
                if (useEnhancedColorsSetting == ColorSchemeOptions.UseEnhancedColors.Migrated)
                {
                    return;
                }

                var colorScheme = useEnhancedColorsSetting == ColorSchemeOptions.UseEnhancedColors.DoNotUse
                    ? ColorSchemeName.VisualStudio2017
                    : ColorSchemeName.VisualStudio2019;

                _globalOptions.SetGlobalOption(new OptionKey(ColorSchemeOptions.ColorScheme), colorScheme);
                _globalOptions.SetGlobalOption(new OptionKey(ColorSchemeOptions.LegacyUseEnhancedColors), ColorSchemeOptions.UseEnhancedColors.Migrated);
            }
        }
    }
}
