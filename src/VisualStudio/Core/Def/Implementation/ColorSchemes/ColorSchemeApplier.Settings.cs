// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
            private const string ColorSchemeApplierKey = @"Roslyn\ColorSchemeApplier";
            private const string AppliedColorSchemeName = "AppliedColorScheme";

            private readonly IServiceProvider _serviceProvider;
            private readonly VisualStudioWorkspace _workspace;

            public ColorSchemeSettings(IServiceProvider serviceProvider, VisualStudioWorkspace visualStudioWorkspace)
            {
                _serviceProvider = serviceProvider;
                _workspace = visualStudioWorkspace;
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

            public void MigrateToColorSchemeSetting()
            {
                // Get the preview feature flag value.
                var useEnhancedColorsSetting = _workspace.Options.GetOption(ColorSchemeOptions.LegacyUseEnhancedColors);

                // Return if we have already migrated.
                if (useEnhancedColorsSetting == ColorSchemeOptions.UseEnhancedColors.Migrated)
                {
                    return;
                }

                var colorScheme = useEnhancedColorsSetting == ColorSchemeOptions.UseEnhancedColors.DoNotUse
                    ? SchemeName.VisualStudio2017
                    : SchemeName.VisualStudio2019;

                _workspace.SetOptions(_workspace.Options.WithChangedOption(ColorSchemeOptions.ColorScheme, colorScheme));
                _workspace.SetOptions(_workspace.Options.WithChangedOption(ColorSchemeOptions.LegacyUseEnhancedColors, ColorSchemeOptions.UseEnhancedColors.Migrated));
            }
        }
    }
}
