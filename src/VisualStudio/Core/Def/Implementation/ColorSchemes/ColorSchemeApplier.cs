// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [Export(typeof(ColorSchemeApplier))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Analyzable)]
    internal sealed partial class ColorSchemeApplier : ForegroundThreadAffinitizedObject, IWpfTextViewConnectionListener, IDisposable
    {
        private const string RoslynTextEditorRegistryKey = "Themes\\{de3dbbcd-f642-433c-8353-8f1df4370aba}\\Roslyn Text Editor MEF Items";
        private const string UseEnhancedColorsSetting = "WindowManagement.Options.UseEnhancedColorsForManagedLanguages";
        private const string AppliedColorSchemeKeyName = "ColorScheme";

        private readonly IServiceProvider _serviceProvider;
        private readonly ForegroundColorDefaulter _colorApplier;
        private readonly ISettingsManager _settingsManager;

        private bool _isDisposed = false;
        private bool isInitialized = false;

        [ImportingConstructor]
        [Obsolete]
        public ColorSchemeApplier(IThreadingContext threadingContext, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _serviceProvider = serviceProvider;
            _settingsManager = (ISettingsManager)_serviceProvider.GetService(typeof(SVsSettingsPersistenceManager));
            _colorApplier = new ForegroundColorDefaulter(threadingContext, _serviceProvider);
        }

        public void Dispose()
        {
            // Dispose is invoked when the MEF container is disposed. This will be our
            // signal that VS is shutting down and we shouldn't try and perform any work.
            _isDisposed = true;
        }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            AssertIsForeground();

            if (!isInitialized)
            {
                isInitialized = true;

                // We need to update the theme whenever the Editor Color Scheme setting changes or the VS Theme changes.
                _settingsManager.GetSubset(ColorSchemeOptions.SettingKey).SettingChangedAsync += ColorSchemeChanged;
                VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;

                // Check on first connect whether we need to migrate the `useEnhancedColorsSetting` to the new `ColorScheme` setting.
                TryToMigrate();

                QueueColorSchemeUpdate();
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }

        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {
            QueueColorSchemeUpdate();
        }

        private async Task ColorSchemeChanged(object sender, PropertyChangedEventArgs args)
        {
            await QueueColorSchemeUpdate();
        }

        private bool TryToMigrate()
        {
            // Get the preview feature flag value.
            var useEnhancedColorsSetting = _settingsManager.GetValueOrDefault(UseEnhancedColorsSetting, defaultValue: 0);

            // useEnhancedColorsSetting
            //  0 -> use enhanced colors.
            //  1 -> use enhanced colors.
            // -1 -> don't use enhanced colors.
            // -2 -> account migrated to new experience.
            if (useEnhancedColorsSetting == -2)
            {
                return false;
            }

            var themeId = GetThemeId();
            var colorScheme = (useEnhancedColorsSetting != -1 && _colorApplier.AreForegroundColorsDefaultable(themeId))
                ? ColorSchemeOptions.Enhanced
                : ColorSchemeOptions.VisualStudio2017;

            _settingsManager.SetValueAsync(ColorSchemeOptions.SettingKey, colorScheme, isMachineLocal: false);
            _settingsManager.SetValueAsync(UseEnhancedColorsSetting, -2, isMachineLocal: false);

            return true;
        }

        private IVsTask QueueColorSchemeUpdate()
        {
            // Wait until things have settled down from the theme change, since we will potentially be changing theme colors.
            return VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadBackgroundPriority, UpdateColorScheme);
        }

        private void UpdateColorScheme()
        {
            AssertIsForeground();

            // Simply return if we were queued to run during shutdown or the user is in High Contrast mode.
            if (_isDisposed || SystemParameters.HighContrast)
            {
                return;
            }

            // Set Foreground colors to default when possible
            TryToDefaultThemeColors();

            // The color scheme that is currently applied to the registry
            var currentColorScheme = GetCurrentColorScheme();

            // If this is a known theme then, use the users choosen option. For unknown themes we default to VS2017
            // since themes would have been created with this as the expected base colors.
            var configuredColorScheme = IsKnownTheme()
                ? _settingsManager.GetValueOrDefault(ColorSchemeOptions.SettingKey, defaultValue: ColorSchemeOptions.Enhanced)
                : ColorSchemeOptions.VisualStudio2017;

            if (currentColorScheme == configuredColorScheme)
            {
                return;
            }

            // Update Default Colors
            SetColorScheme(configuredColorScheme);

            // Broadcast that system color settings have changed to force the ColorThemeService to reload colors.
            NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SYSCOLORCHANGE, wparam: IntPtr.Zero, lparam: IntPtr.Zero);
        }

        private void TryToDefaultThemeColors()
        {
            var themeId = GetThemeId();

            // Do not change for unknown themes or if we've already tried to default colors.
            if (!IsKnownTheme(themeId)
                || HasThemeBeenDefaulted(themeId))
            {
                return;
            }

            // The previous method for applying color schemes updated the color of classifications to the expected theme color. 
            // However, this new method relies on theme colors being the default, so classifications should use DefaultColor when
            // possible.
            if (_colorApplier.TrySetForegroundColorsToDefault(themeId))
            {
                SetThemeWasDefaulted(themeId);
            }
        }

        private bool HasThemeBeenDefaulted(Guid themeId)
        {
            using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: true);
            using var textEditorKey = registryRoot.CreateSubKey(RoslynTextEditorRegistryKey);
            return (int)textEditorKey.GetValue(GetIsThemeDefaultedKeyName(themeId), 0) == 1;
        }

        private void SetThemeWasDefaulted(Guid themeId)
        {
            using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: true);
            using var textEditorKey = registryRoot.CreateSubKey(RoslynTextEditorRegistryKey);
            textEditorKey.SetValue(GetIsThemeDefaultedKeyName(themeId), 1);
        }

        private string GetIsThemeDefaultedKeyName(Guid themeId)
        {
            return $"IsThemeDefaulted.{themeId}";
        }

        private void SetColorScheme(string colorSchemeName)
        {
            // Currently, this is the only subsitition token supported here.
            const string RootKeyToken = "$RootKey$";

            using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: true);

            // The below code is not a general purpose PkgDef merge utility, but one that only
            // supports the "color theme" format.
            using (var colorSchemeStream = GetColorSchemePackageDefStream(colorSchemeName))
            using (var reader = new PkgDefFileReader(new StreamReader(colorSchemeStream)))
            {
                PkgDefItem? item;
                while ((item = reader.Read()) != null)
                {
                    if ((item.ValueType == PkgDefItem.PkgDefValueType.Binary ||
                            item.ValueType == PkgDefItem.PkgDefValueType.DWord) &&
                        item.SectionName.StartsWith(RootKeyToken) && item.SectionName[RootKeyToken.Length] == '\\')
                    {
                        var itemRoot = item.SectionName.Substring(RootKeyToken.Length + 1);
                        using var itemKey = registryRoot.CreateSubKey(itemRoot);
                        itemKey.SetValue(item.ValueName, item.ValueData);
                    }
                }
            }

            // Set the current Color Scheme in the registry
            using (var itemKey = registryRoot.CreateSubKey(RoslynTextEditorRegistryKey))
            {
                itemKey.SetValue(AppliedColorSchemeKeyName, colorSchemeName);
            }
        }

        private Stream GetColorSchemePackageDefStream(string colorSchemeName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceStream($"Microsoft.VisualStudio.LanguageServices.{colorSchemeName}.pkgdef");
        }

        private string GetCurrentColorScheme()
        {
            using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: true);
            using var textEditorKey = registryRoot.CreateSubKey(RoslynTextEditorRegistryKey);
            return (string)textEditorKey.GetValue(AppliedColorSchemeKeyName, defaultValue: string.Empty);
        }

        public bool IsKnownTheme()
        {
            return IsKnownTheme(GetThemeId());
        }

        public bool IsThemeCustomized()
        {
            return !_colorApplier.AreForegroundColorsDefaultable(GetThemeId());
        }

        public static bool IsKnownTheme(Guid currentTheme)
        {
            return currentTheme == KnownColorThemes.Light ||
                currentTheme == KnownColorThemes.Blue ||
                currentTheme == KnownColorThemes.AdditionalContrast ||
                currentTheme == KnownColorThemes.Dark;
        }

        private Guid GetThemeId()
        {
            const string CurrentThemeValueName = "Microsoft.VisualStudio.ColorTheme";
            const string CurrentThemeValueNameNew = "Microsoft.VisualStudio.ColorThemeNew";

            // Look up the value from the new roamed theme property first and
            // fallback to the original roamed theme property if that fails.
            var themeIdString = _settingsManager.GetValueOrDefault<string>(CurrentThemeValueNameNew)
                ?? _settingsManager.GetValueOrDefault<string>(CurrentThemeValueName);

            return Guid.TryParse(themeIdString, out var themeId) ? themeId : Guid.Empty;
        }

        // NOTE: This service is not public or intended for use by teams/individuals outside of Microsoft. Any data stored is subject to deletion without warning.
        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { };
    }
}
