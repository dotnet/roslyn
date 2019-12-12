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
    internal partial class ColorSchemeApplier : ForegroundThreadAffinitizedObject, IWpfTextViewConnectionListener, IDisposable
    {
        private const string RoslynTextEditorRegistryKey = "Themes\\{de3dbbcd-f642-433c-8353-8f1df4370aba}\\Roslyn Text Editor MEF Items";
        private const string UseEnhancedColorsSetting = "WindowManagement.Options.UseEnhancedColorsForManagedLanguages";

        private readonly IServiceProvider _serviceProvider;
        private readonly ForegroundColorDefaulter _colorApplier;
        private readonly ISettingsManager _settingsManager;
        private readonly object _colorThemeService;

        private bool _isDisposed = false;
        private bool _hasTextViewOpened = false;

        [ImportingConstructor]
        [Obsolete]
        public ColorSchemeApplier(IThreadingContext threadingContext, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _serviceProvider = serviceProvider;
            _settingsManager = (ISettingsManager)_serviceProvider.GetService(typeof(SVsSettingsPersistenceManager));
            _colorThemeService = _serviceProvider.GetService(typeof(SVsColorThemeService));
            _colorApplier = new ForegroundColorDefaulter(_serviceProvider);
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

            if (!_hasTextViewOpened)
            {
                _hasTextViewOpened = true;

                // We need to update the theme whenever the Preview Setting changes or the VS Theme changes.
                _settingsManager.GetSubset(ColorSchemeOptions.SettingKey).SettingChangedAsync += ColorSchemeChanged;
                VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;

                // Check on first connect whether we need to migrate the `useEnhancedColorsSetting` to the new `ColorScheme` setting.
                TryToMigrate();

                VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadBackgroundPriority, UpdateColorScheme);
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }

        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {
            // Wait until things have settled down from the theme change, since we will potentially be changing theme colors.
            VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadBackgroundPriority, UpdateColorScheme);
        }

        private async Task ColorSchemeChanged(object sender, PropertyChangedEventArgs args)
        {
            await VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadBackgroundPriority, UpdateColorScheme);
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
            var colorScheme = (useEnhancedColorsSetting != -1 && _colorApplier.AreClassificationsDefaulted(themeId))
                ? ColorSchemeOptions.Enhanced
                : ColorSchemeOptions.VisualStudio2017;

            _settingsManager.SetValueAsync(ColorSchemeOptions.SettingKey, colorScheme, false);
            _settingsManager.SetValueAsync(UseEnhancedColorsSetting, -2, false);

            return true;
        }

        private void UpdateColorScheme()
        {
            AssertIsForeground();

            // Simply return if we were queued to run during shutdown.
            if (_isDisposed)
            {
                return;
            }

            // Set Foreground colors to default when possible
            TryToDefaultThemeColors();

            // The color scheme that is currently applied to the registry
            var currentColorScheme = GetCurrentColorScheme();

            // If this is a known theme then, use the users choosen option. For unknown themes we default to VS2017
            // since themes would have been created with this as the expected base colors.
            var configuredColorScheme = IsKnowTheme()
                ? _settingsManager.GetValueOrDefault(ColorSchemeOptions.SettingKey, defaultValue: ColorSchemeOptions.Enhanced)
                : ColorSchemeOptions.VisualStudio2017;

            if (currentColorScheme == configuredColorScheme)
            {
                return;
            }

            // Update Default Colors
            SetColorScheme(configuredColorScheme);

            // Cause the Color Theme Service to reload colors.
            var themes = _colorThemeService.GetType().GetField("_themes", BindingFlags.Instance | BindingFlags.NonPublic);
            themes.SetValue(_colorThemeService, null);
            NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SYSCOLORCHANGE, IntPtr.Zero, IntPtr.Zero);
        }

        private void TryToDefaultThemeColors()
        {
            var themeId = GetThemeId();

            // Do not change when in HighContrast mode, for unknown themes, or if we've already tried to default colors.
            if (SystemParameters.HighContrast
                || !IsKnowTheme(themeId)
                || HasThemeBeenDefaulted(themeId))
            {
                return;
            }

            // The previous method for applying color schemes updated the color of classifications to the expected theme color. 
            // However, this new method relies on theme colors being the default, so classifications should use DefaultColor when
            // possible.
            _colorApplier.SetDefaultForeground(themeId);
            SetThemeWasDefaulted(themeId);
        }

        private bool HasThemeBeenDefaulted(Guid themeId)
        {
            using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: true);
            using var textEditorKey = registryRoot.CreateSubKey(RoslynTextEditorRegistryKey);
            return (int)textEditorKey.GetValue(GetDefaultedThemeGuid(themeId), 0) == 1;
        }

        private void SetThemeWasDefaulted(Guid themeId)
        {
            using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: true);
            using var textEditorKey = registryRoot.CreateSubKey(RoslynTextEditorRegistryKey);
            textEditorKey.SetValue(GetDefaultedThemeGuid(themeId), 1);
        }

        private string GetDefaultedThemeGuid(Guid themeId)
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
                itemKey.SetValue("ColorScheme", colorSchemeName);
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
            return (string)textEditorKey.GetValue("ColorScheme", string.Empty);
        }

        public bool IsKnowTheme()
        {
            return IsKnowTheme(GetThemeId());
        }

        public bool IsKnowTheme(Guid currentTheme)
        {
            return currentTheme == KnownColorThemes.Light ||
                currentTheme == KnownColorThemes.Blue ||
                currentTheme == KnownColorThemes.AdditionalContrast ||
                currentTheme == KnownColorThemes.Dark ||
                currentTheme == KnownColorThemes.HighContrast;
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
        [Guid("0D915B59-2ED7-472A-9DE8-9161737EA1C5")]
        private class SVsColorThemeService { };

        // NOTE: This service is not public or intended for use by teams/individuals outside of Microsoft. Any data stored is subject to deletion without warning.
        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { };
    }
}
