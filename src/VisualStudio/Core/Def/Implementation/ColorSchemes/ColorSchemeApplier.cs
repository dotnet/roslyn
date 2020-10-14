// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.ColorSchemes;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    [Export(typeof(ColorSchemeApplier))]
    internal sealed partial class ColorSchemeApplier : ForegroundThreadAffinitizedObject, IDisposable
    {
        private const string ColorThemeValueName = "Microsoft.VisualStudio.ColorTheme";
        private const string ColorThemeNewValueName = "Microsoft.VisualStudio.ColorThemeNew";

        private readonly IServiceProvider _serviceProvider;
        private readonly ColorSchemeSettings _settings;
        private readonly ClassificationVerifier _classificationVerifier;
        private readonly ImmutableDictionary<SchemeName, ColorScheme> _colorSchemes;
        private readonly AsyncLazy<ImmutableDictionary<SchemeName, ImmutableArray<RegistryItem>>> _colorSchemeRegistryItems;

        private bool _isInitialized = false;
        private bool _isDisposed = false;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ColorSchemeApplier(
            IThreadingContext threadingContext,
            VisualStudioWorkspace visualStudioWorkspace,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _serviceProvider = serviceProvider;

            _settings = new ColorSchemeSettings(_serviceProvider, visualStudioWorkspace);
            _colorSchemes = _settings.GetColorSchemes();
            _classificationVerifier = new ClassificationVerifier(threadingContext, serviceProvider, _colorSchemes);

            _colorSchemeRegistryItems = new AsyncLazy<ImmutableDictionary<SchemeName, ImmutableArray<RegistryItem>>>(GetColorSchemeRegistryItemsAsync, cacheResult: true);
        }

        public void Dispose()
        {
            // Dispose is invoked when the MEF container is disposed. This will be our
            // signal that VS is shutting down and we shouldn't try and perform any work.
            _isDisposed = true;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            AssertIsForeground();

            _isInitialized = true;

            _ = _colorSchemeRegistryItems.GetValueAsync(CancellationToken.None);

            // We need to update the theme whenever the Editor Color Scheme setting changes or the VS Theme changes.
            var settingsManager = (ISettingsManager)_serviceProvider.GetService(typeof(SVsSettingsPersistenceManager));
            settingsManager.GetSubset(ColorSchemeOptions.ColorSchemeSettingKey).SettingChangedAsync += ColorSchemeChangedAsync;

            VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;

            // Try to migrate the `useEnhancedColorsSetting` to the new `ColorScheme` setting.
            _settings.MigrateToColorSchemeSetting();

            // Since the Roslyn colors are now defined in the Roslyn repo and no longer applied by the VS pkgdef built from EditorColors.xml,
            // We attempt to apply a color scheme when the Roslyn package is loaded. This is our chance to update the configuration registry
            // with the Roslyn colors before they are seen by the user. This is important because the MEF exported Roslyn classification
            // colors are only applicable to the Blue and Light VS themes.

            UpdateColorScheme();
        }

        private Task<ImmutableDictionary<SchemeName, ImmutableArray<RegistryItem>>> GetColorSchemeRegistryItemsAsync(CancellationToken arg)
            => SpecializedTasks.FromResult(_colorSchemes.ToImmutableDictionary(kvp => kvp.Key, kvp => RegistryItemConverter.Convert(kvp.Value)));

        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
            => QueueColorSchemeUpdate();

        private async Task ColorSchemeChangedAsync(object sender, PropertyChangedEventArgs args)
            => await QueueColorSchemeUpdate();

        private IVsTask QueueColorSchemeUpdate()
        {
            // Wait until things have settled down from the theme change, since we will potentially be changing theme colors.
            return VsTaskLibraryHelper.CreateAndStartTask(
                VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadBackgroundPriority, () => UpdateColorScheme());
        }

        private void UpdateColorScheme()
        {
            AssertIsForeground();

            // Simply return if we were queued to run during shutdown.
            if (_isDisposed)
            {
                return;
            }

            // If the color scheme has updated, apply the scheme.
            if (TryGetUpdatedColorScheme(out var colorScheme))
            {
                var colorSchemeRegistryItems = _colorSchemeRegistryItems.GetValue(CancellationToken.None);
                _settings.ApplyColorScheme(colorScheme.Value, colorSchemeRegistryItems[colorScheme.Value]);
            }
        }

        /// <summary>
        /// Returns true if the color scheme needs updating.
        /// </summary>
        /// <param name="colorScheme">The color scheme to update with.</param>
        private bool TryGetUpdatedColorScheme([NotNullWhen(returnValue: true)] out SchemeName? colorScheme)
        {
            // The color scheme that is currently applied to the registry
            var appliedColorScheme = _settings.GetAppliedColorScheme();

            // If this is a supported theme then, use the users configured scheme, otherwise fallback to the VS 2017.
            // Custom themes would be based on the MEF exported color information for classifications which matches the VS 2017 theme.
            var configuredColorScheme = IsSupportedTheme()
                ? _settings.GetConfiguredColorScheme()
                : SchemeName.VisualStudio2017;

            if (appliedColorScheme == configuredColorScheme)
            {
                colorScheme = null;
                return false;
            }

            colorScheme = configuredColorScheme;
            return true;
        }

        public bool IsSupportedTheme()
            => IsSupportedTheme(GetThemeId());

        public bool IsSupportedTheme(Guid themeId)
        {
            return _colorSchemes.Values.Any(
                scheme => scheme.Themes.Any(
                    theme => theme.Guid == themeId));
        }

        public bool IsThemeCustomized()
            => _classificationVerifier.AreForegroundColorsCustomized(_settings.GetConfiguredColorScheme(), GetThemeId());

        public Guid GetThemeId()
        {
            AssertIsForeground();

            var settingsManager = (ISettingsManager)_serviceProvider.GetService(typeof(SVsSettingsPersistenceManager));

            //  Look up the value from the new roamed theme property first
            //  Fallback to the original roamed theme property if that fails
            var currentThemeString = settingsManager.GetValueOrDefault<string?>(ColorThemeNewValueName, defaultValue: null) ??
                settingsManager.GetValueOrDefault<string?>(ColorThemeValueName, defaultValue: null);

            if (currentThemeString is null)
            {
                // The ColorTheme setting is unpopulated when it has never been changed from its default.
                // The default VS ColorTheme is Blue
                return KnownColorThemes.Blue;
            }

            return Guid.Parse(currentThemeString);
        }

        // NOTE: This service is not public or intended for use by teams/individuals outside of Microsoft. Any data stored is subject to deletion without warning.
        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { }
    }
}
