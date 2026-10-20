// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Settings;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

internal sealed class NamingSettingsRuntime : IDisposable
{
    private readonly NamingSettingsStore _store;
    private readonly ISettingsSubset _legacySubset;
    private readonly string _legacyKey;
    private readonly string _fallbackLegacyKey;
    private readonly Func<NamingStylePreferences> _readLegacy;
    private readonly Action<Task> _observe;
    private readonly Action<Exception> _reportRetryFailure;
    private readonly CancellationTokenSource _lifetime;
    private readonly CancellationToken _cancellationToken;
    private readonly TimeSpan? _retryDelay;
    private readonly object _initializationGate = new();
    private readonly object _legacyChangeGate = new();
    private readonly TaskCompletionSource<bool> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private IDisposable? _subscription;
    private Task? _initialization;
    private Task _legacyChanges = Task.CompletedTask;
    private bool _disposed;

    public NamingSettingsCoordinator Coordinator { get; }
    public NamingSettingsProvider Provider { get; }
    public Task Ready => _ready.Task;

    public NamingSettingsRuntime(NamingSettingsStore store, NamingSettingsCoordinator coordinator,
        ISettingsSubset legacySubset, string legacyKey, string fallbackLegacyKey, Func<NamingStylePreferences> readLegacy,
        Action<Task> observe, Action<Exception> reportRetryFailure, CancellationToken cancellationToken, TimeSpan? retryDelay = null)
    {
        _store = store;
        Coordinator = coordinator;
        Provider = new(coordinator, store.Language);
        _legacySubset = legacySubset;
        _legacyKey = legacyKey;
        _fallbackLegacyKey = fallbackLegacyKey;
        _readLegacy = readLegacy;
        _observe = observe;
        _reportRetryFailure = reportRetryFailure;
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationToken = _lifetime.Token;
        _retryDelay = retryDelay;
    }

    public Task InitializeAsync()
    {
        lock (_initializationGate)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NamingSettingsRuntime));
            return _initialization ??= InitializeCoreAsync();
        }
    }

    private async Task InitializeCoreAsync()
    {
        try
        {
            // Classic mode continues to use the existing legacy option persister without native synchronization.
            if (!_store.IsUnifiedSettingsEnabled)
                return;
            _subscription = _store.SubscribeToChanges(update =>
            {
                if (!Coordinator.IsOwnChange(update))
                    _observe(RefreshNativeAsync());
            });
            _legacySubset.SettingChangedAsync += OnLegacyChangedAsync;
            await Coordinator.SynchronizeFromUnifiedSettingsAsync(_cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Release callbacks after the attempt, even on failure, so a repaired file can recover the region.
            _ready.TrySetResult(true);
        }
    }

    private async Task RefreshNativeAsync()
    {
        await Ready.ConfigureAwait(false);
        Coordinator.NotifyChanged();
        await Coordinator.SynchronizeFromUnifiedSettingsAsync(_cancellationToken).ConfigureAwait(false);
        Provider.NotifyErrorConditionResolved();
    }

    private Task OnLegacyChangedAsync(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == _legacyKey || args.PropertyName == _fallbackLegacyKey)
            _observe(RefreshLegacyAsync());
        return Task.CompletedTask;
    }

    private async Task RefreshLegacyAsync()
    {
        NamingStylePreferences preferences;
        Task previous;
        TaskCompletionSource<bool> completed;
        lock (_legacyChangeGate)
        {
            preferences = _readLegacy();
            if (Coordinator.IsOwnLegacyChange(preferences))
                return;
            previous = _legacyChanges;
            completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _legacyChanges = completed.Task;
        }

        try
        {
            await previous.ConfigureAwait(false);
            await Ready.ConfigureAwait(false);
            await SettingsSynchronizationRetry.RunAsync(
                () => Coordinator.SynchronizeFromLegacyAsync(preferences, _cancellationToken),
                _reportRetryFailure, _cancellationToken, _retryDelay).ConfigureAwait(false);
            Provider.NotifyErrorConditionResolved();
        }
        finally
        {
            // Release the next import even on failure; the callback task reports its own error.
            completed.SetResult(true);
        }
    }

    public void Dispose()
    {
        lock (_initializationGate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _lifetime.Cancel();
            _subscription?.Dispose();
            _legacySubset.SettingChangedAsync -= OnLegacyChangedAsync;
            Provider.Dispose();
            _lifetime.Dispose();
        }
    }
}
