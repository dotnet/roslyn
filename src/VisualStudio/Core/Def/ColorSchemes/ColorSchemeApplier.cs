// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.ColorSchemes;

[Export(typeof(ColorSchemeApplier))]
internal sealed partial class ColorSchemeApplier
{
    private const string ColorThemeValueName = "Microsoft.VisualStudio.ColorTheme";
    private const string ColorThemeNewValueName = "Microsoft.VisualStudio.ColorThemeNew";

    private readonly IThreadingContext _threadingContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAsyncServiceProvider _asyncServiceProvider;
    private readonly ColorSchemeSettings _settings;
    private readonly ClassificationVerifier _classificationVerifier;
    private readonly ImmutableDictionary<ColorSchemeName, ColorScheme> _colorSchemes;

    private readonly object _gate = new();

    private ImmutableDictionary<ColorSchemeName, ImmutableArray<RegistryItem>>? _colorSchemeRegistryItems;
    private bool _isInitialized = false;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ColorSchemeApplier(
        IThreadingContext threadingContext,
        IVsService<SVsFontAndColorStorage, IVsFontAndColorStorage> fontAndColorStorage,
        IGlobalOptionService globalOptions,
        SVsServiceProvider serviceProvider)
    {
        _threadingContext = threadingContext;
        _serviceProvider = serviceProvider;
        _asyncServiceProvider = (IAsyncServiceProvider)serviceProvider;

        _settings = new ColorSchemeSettings(threadingContext, _serviceProvider, globalOptions);
        _colorSchemes = ColorSchemeSettings.GetColorSchemes();
        _classificationVerifier = new ClassificationVerifier(threadingContext, fontAndColorStorage, _colorSchemes);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
        }

        // We need to update the theme whenever the Editor Color Scheme setting changes or the VS Theme changes.
        await TaskScheduler.Default;
        var settingsManager = await _asyncServiceProvider.GetServiceAsync<SVsSettingsPersistenceManager, ISettingsManager>(_threadingContext.JoinableTaskFactory).ConfigureAwait(false);

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        settingsManager.GetSubset(ColorSchemeOptionsStorage.ColorSchemeSettingKey).SettingChangedAsync += ColorSchemeChangedAsync;
        VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;

        await TaskScheduler.Default;

        // Try to migrate the `useEnhancedColorsSetting` to the new `ColorSchemeName` setting.
        _settings.MigrateToColorSchemeSetting();

        // Since the Roslyn colors are now defined in the Roslyn repo and no longer applied by the VS pkgdef built from EditorColors.xml,
        // We attempt to apply a color scheme when the Roslyn package is loaded. This is our chance to update the configuration registry
        // with the Roslyn colors before they are seen by the user. This is important because the MEF exported Roslyn classification
        // colors are only applicable to the Blue and Light VS themes.

        // If the color scheme has updated, apply the scheme.
        await UpdateColorSchemeAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateColorSchemeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var colorScheme = await TryGetUpdatedColorSchemeAsync(cancellationToken).ConfigureAwait(false);
        if (colorScheme == null)
            return;

        _colorSchemeRegistryItems ??= _colorSchemes.ToImmutableDictionary(
            kvp => kvp.Key, kvp => RegistryItemConverter.Convert(kvp.Value));

        await _settings.ApplyColorSchemeAsync(
            colorScheme.Value, _colorSchemeRegistryItems[colorScheme.Value], cancellationToken).ConfigureAwait(false);
    }

    private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        => QueueColorSchemeUpdateAsync().Forget();

    private Task ColorSchemeChangedAsync(object sender, PropertyChangedEventArgs args)
        => QueueColorSchemeUpdateAsync();

    private async Task QueueColorSchemeUpdateAsync()
    {
        // Wait until things have settled down from the theme change, since we will potentially be changing theme colors.
        await VsTaskLibraryHelper.StartOnIdle(_threadingContext.JoinableTaskFactory, () => UpdateColorSchemeAsync(_threadingContext.DisposalToken));
    }

    /// <summary>
    /// Returns true if the color scheme needs updating.
    /// </summary>
    private async Task<ColorSchemeName?> TryGetUpdatedColorSchemeAsync(CancellationToken cancellationToken)
    {
        // The color scheme that is currently applied to the registry
        var appliedColorScheme = await _settings.GetAppliedColorSchemeAsync(cancellationToken).ConfigureAwait(false);

        // If this is a supported theme then, use the users configured scheme, otherwise fallback to the VS 2017.
        // Custom themes would be based on the MEF exported color information for classifications which matches the VS 2017 theme.
        var configuredColorScheme = await IsSupportedThemeAsync(cancellationToken).ConfigureAwait(false)
            ? _settings.GetConfiguredColorScheme()
            : ColorSchemeName.VisualStudio2017;

        if (appliedColorScheme == configuredColorScheme)
            return null;

        return configuredColorScheme;
    }

    public async Task<bool> IsSupportedThemeAsync(CancellationToken cancellationToken)
        => IsSupportedTheme(await GetThemeIdAsync(cancellationToken).ConfigureAwait(false));

    private bool IsSupportedTheme(Guid themeId)
    {
        return _colorSchemes.Values.Any(
            scheme => scheme.Themes.Any(
                static (theme, themeId) => theme.Guid == themeId, themeId));
    }

    public async Task<bool> IsThemeCustomizedAsync(CancellationToken cancellationToken)
        => await _classificationVerifier.AreForegroundColorsCustomizedAsync(
            _settings.GetConfiguredColorScheme(),
            await GetThemeIdAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

    private async Task<Guid> GetThemeIdAsync(CancellationToken cancellationToken)
    {
        await TaskScheduler.Default;
        var settingsManager = await _asyncServiceProvider.GetServiceAsync<SVsSettingsPersistenceManager, ISettingsManager>(_threadingContext.JoinableTaskFactory).ConfigureAwait(false);
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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
