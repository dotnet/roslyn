// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.ColorSchemes;

[Export(typeof(ColorSchemeApplier))]
internal sealed partial class ColorSchemeApplier
{
    private readonly IThreadingContext _threadingContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAsyncServiceProvider _asyncServiceProvider;
    private readonly ColorSchemeSettings _settings;
    private readonly ImmutableDictionary<ColorSchemeName, ColorScheme> _colorSchemes;
    private readonly AsyncBatchingWorkQueue _workQueue;

    private readonly object _gate = new();

    private ImmutableDictionary<ColorSchemeName, ImmutableArray<RegistryItem>>? _colorSchemeRegistryItems;
    private bool _isInitialized = false;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ColorSchemeApplier(
        IThreadingContext threadingContext,
        IVsService<SVsFontAndColorStorage, IVsFontAndColorStorage> fontAndColorStorage,
        IGlobalOptionService globalOptions,
        SVsServiceProvider serviceProvider,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _threadingContext = threadingContext;
        _serviceProvider = serviceProvider;
        _asyncServiceProvider = (IAsyncServiceProvider)serviceProvider;

        _settings = new ColorSchemeSettings(threadingContext, _serviceProvider, globalOptions);
        _colorSchemes = ColorSchemeSettings.GetColorSchemes();
        _workQueue = new(
            DelayTimeSpan.Idle,
            QueueColorSchemeUpdateAsync,
            listenerProvider.GetListener(FeatureAttribute.ColorScheme),
            threadingContext.DisposalToken);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
        }

        // We need to update the theme whenever the Editor Color Scheme setting changes.
        await TaskScheduler.Default;
        var settingsManager = await _asyncServiceProvider.GetServiceAsync<SVsSettingsPersistenceManager, ISettingsManager>(_threadingContext.JoinableTaskFactory).ConfigureAwait(false);

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        settingsManager.GetSubset(ColorSchemeOptionsStorage.ColorSchemeSettingKey).SettingChangedAsync += ColorSchemeChangedAsync;

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

    private Task ColorSchemeChangedAsync(object sender, PropertyChangedEventArgs args)
    {
        _workQueue.AddWork();
        return Task.CompletedTask;
    }

    private async ValueTask QueueColorSchemeUpdateAsync(CancellationToken cancellationToken)
    {
        // Wait until things have settled down from the theme change, since we will potentially be changing theme colors.
        await VsTaskLibraryHelper.StartOnIdle(_threadingContext.JoinableTaskFactory, () => UpdateColorSchemeAsync(cancellationToken));
    }

    /// <summary>
    /// Returns a non-null value, if the color scheme needs updating.
    /// </summary>
    private async Task<ColorSchemeName?> TryGetUpdatedColorSchemeAsync(CancellationToken cancellationToken)
    {
        // The color scheme that is currently applied to the registry
        var appliedColorScheme = await _settings.GetAppliedColorSchemeAsync(cancellationToken).ConfigureAwait(false);

        // The color scheme configured in VS settings.
        var configuredColorScheme = _settings.GetConfiguredColorScheme();

        if (appliedColorScheme == configuredColorScheme)
            return null;

        return configuredColorScheme;
    }

    // NOTE: This service is not public or intended for use by teams/individuals outside of Microsoft. Any data stored is subject to deletion without warning.
    [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
    private class SVsSettingsPersistenceManager { }
}
