// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.RpcContracts.UnifiedSettings;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

/// <summary>
/// Synchronizes ordinary preference/severity settings with the compound XML used by profiles and roaming.
/// All pair updates are serialized, and each write reads the current compound value before changing either half.
/// </summary>
internal sealed class NativeCodeStyleSettingsSynchronizer : IObserver<SettingsUpdate>, IDisposable
{
    private static readonly ChangeSource s_source = new(nameof(NativeCodeStyleSettingsSynchronizer), new Guid("3a6e0fd5-85fb-45f1-9ef6-7cf5d21595db"));
    private readonly IUnifiedSettingsService _native;
    private readonly VisualStudioSettingsOptionPersister _legacy;
    private readonly ISettingsSubset _legacySubset;
    private readonly Dictionary<string, Binding> _bindings;
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ICodeStyleOption2> _legacyValues = new(StringComparer.Ordinal);
    private readonly ReentrantSemaphore _gate;
    private readonly Action<OptionKey2, object?> _refresh;
    private readonly Action<Task> _observe;
    private readonly Action<Task> _reportRetryFailure;
    private readonly CancellationToken _cancellationToken;
    private readonly TimeSpan? _retryDelay;
    private IDisposable? _subscription;
    private LegacyChange? _publishingLegacy;
    private int _initialSnapshot;

    internal sealed record Binding(CodeStyleSetting Setting, string LegacyKey, string RowMoniker)
    {
        public string Moniker => RowMoniker + ".preference";
        public string SeverityMoniker => RowMoniker + ".severity";
    }

    private sealed record LegacyChange(Binding Binding, ICodeStyleOption2 Value);

    public NativeCodeStyleSettingsSynchronizer(
        IUnifiedSettingsService native,
        VisualStudioSettingsOptionPersister legacy,
        IEnumerable<Binding> bindings,
        Action<OptionKey2, object?> refresh,
        Action<Task> observe,
        JoinableTaskContext joinableTaskContext,
        CancellationToken cancellationToken,
        TimeSpan? retryDelay = null,
        Action<Task>? reportRetryFailure = null)
    {
        _native = native;
        _legacy = legacy;
        _bindings = bindings.ToDictionary(binding => binding.Moniker, StringComparer.Ordinal);
        _refresh = refresh;
        _observe = observe;
        _reportRetryFailure = reportRetryFailure ?? observe;
        _gate = ReentrantSemaphore.Create(1, joinableTaskContext, ReentrantSemaphore.ReentrancyMode.NotRecognized);
        _cancellationToken = cancellationToken;
        _retryDelay = retryDelay;
        _legacySubset = legacy.SettingsManager.GetSubset("*");
    }

    internal Task InitializeAsync()
        => _gate.ExecuteAsync(async () =>
        {
            var patterns = _bindings.Values.SelectMany(binding => new[] { binding.Moniker, binding.SeverityMoniker }).ToArray();
            _subscription = await _native.SubscribeAsync(this, patterns, ReadOptions.NoFallbackToDefault, _cancellationToken).ConfigureAwait(false);
            _legacySubset.SettingChangedAsync += OnLegacyChangedAsync;
            var persisted = (await _native.GetValuesAsync(patterns, ReadOptions.NoFallbackToDefault, _cancellationToken).ConfigureAwait(false))
                .ToDictionary(result => result.Moniker, StringComparer.Ordinal);
            var effective = (await _native.GetValuesAsync(patterns, ReadOptions.NoRequirements, _cancellationToken).ConfigureAwait(false))
                .ToDictionary(result => result.Moniker, StringComparer.Ordinal);
            var seed = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var binding in _bindings.Values)
            {
                var current = ReadLegacy(binding, out var hasLegacyValue);
                if (hasLegacyValue)
                    _legacyValues[binding.Moniker] = current;
                var merged = current;
                var hasNativeValue = false;
                foreach (var severity in new[] { false, true })
                {
                    var moniker = severity ? binding.SeverityMoniker : binding.Moniker;
                    if (persisted.TryGetValue(moniker, out var result) && result.IsSuccess)
                    {
                        hasNativeValue = true;
                        var scalar = JsonConvert.DeserializeObject<string>(result.Value!.Data)!;
                        merged = (ICodeStyleOption2)binding.Setting.Update(merged, scalar, severity);
                    }
                    else if (hasLegacyValue)
                    {
                        seed[moniker] = JsonConvert.SerializeObject(GetScalar(binding, current, severity));
                    }
                    else if (effective.TryGetValue(moniker, out var defaultResult) && defaultResult.IsSuccess)
                    {
                        var scalar = JsonConvert.DeserializeObject<string>(defaultResult.Value!.Data)!;
                        merged = (ICodeStyleOption2)binding.Setting.Update(merged, scalar, severity);
                    }
                }
                if (!merged.Equals(current))
                {
                    if (hasNativeValue || hasLegacyValue)
                        await WriteLegacyAsync(binding, merged).ConfigureAwait(false);
                    else
                        _refresh(binding.Setting.Key, merged);
                }
                _values[binding.Moniker] = GetScalar(binding, merged, severity: false);
                _values[binding.SeverityMoniker] = GetScalar(binding, merged, severity: true);
            }
            // Re-read before seeding so imports made while the legacy snapshot was assembled take precedence.
            foreach (var result in seed.Count == 0 ? Array.Empty<ReadResult>() : await _native.GetValuesAsync(seed.Keys.ToArray(), ReadOptions.NoFallbackToDefault, _cancellationToken).ConfigureAwait(false))
            {
                if (result.IsSuccess)
                    seed.Remove(result.Moniker);
            }
            await CommitAsync(seed).ConfigureAwait(false);
        }, _cancellationToken);

    private ICodeStyleOption2 ReadLegacy(Binding binding, out bool hasValue)
    {
        hasValue = _legacy.TryFetch(binding.Setting.Key, binding.LegacyKey, out var value);
        return (ICodeStyleOption2)(hasValue ? value! : binding.Setting.Key.Option.DefaultValue!);
    }

    private static string GetScalar(Binding binding, ICodeStyleOption2 style, bool severity)
        => severity ? CodeStyleSetting.SerializeSeverity(style.Notification) : binding.Setting.SerializePreference(style);

    private async Task WriteLegacyAsync(Binding binding, ICodeStyleOption2 value)
    {
        // XML normalizes suppressed severity to Hidden. Suppress our own notification using that value,
        // without re-reading storage that a concurrent profile import may already have changed.
        var storedValue = value.Notification.Severity == Microsoft.CodeAnalysis.ReportDiagnostic.Suppress
            ? value.WithNotification(NotificationOption2.Silent) : value;
        Volatile.Write(ref _publishingLegacy, new(binding, storedValue));
        try
        {
            await _legacy.PersistAsync(binding.Setting.Key, binding.LegacyKey, value).ConfigureAwait(false);
            _legacyValues[binding.Moniker] = storedValue;
            _refresh(binding.Setting.Key, value);
        }
        finally
        {
            Volatile.Write(ref _publishingLegacy, null);
        }
    }

    private async Task CommitAsync(Dictionary<string, string> changes)
    {
        if (changes.Count == 0)
            return;
        var result = await _native.RequestCommitAsync(s_source, new SettingsCommit(changes, nameof(NativeCodeStyleSettingsSynchronizer)), _cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            throw new InvalidOperationException("Code style settings commit failed: " + result.ResultCode);
    }

    private Task OnLegacyChangedAsync(object sender, PropertyChangedEventArgs args)
    {
        // SettingsManager may await its handlers during SetValueAsync. Never wait for our own write gate here.
        var publishing = Volatile.Read(ref _publishingLegacy);
        var snapshots = _bindings.Values.Where(binding => binding.LegacyKey == args.PropertyName)
            .Select(binding => new LegacyChange(binding, ReadLegacy(binding, out _)))
            .Where(change => publishing is null || change.Binding.LegacyKey != publishing.Binding.LegacyKey || !change.Value.Equals(publishing.Value))
            .ToArray();
        if (snapshots.Length > 0)
            _observe(ApplyLegacyChangeAsync(snapshots));
        return Task.CompletedTask;
    }

    private Task ApplyLegacyChangeAsync(LegacyChange[] snapshots)
        => _gate.ExecuteAsync(() => SettingsSynchronizationRetry.RunAsync(async () =>
        {
            var changes = new Dictionary<string, string>(StringComparer.Ordinal);
            var pending = new List<(Binding binding, ICodeStyleOption2 value)>(snapshots.Length);
            foreach (var (binding, value) in snapshots)
            {
                if (_legacyValues.TryGetValue(binding.Moniker, out var previousLegacyValue) &&
                    value.Equals(previousLegacyValue) &&
                    NativeValueMatches(binding, value))
                    continue;
                pending.Add((binding, value));
                foreach (var severity in new[] { false, true })
                {
                    var moniker = severity ? binding.SeverityMoniker : binding.Moniker;
                    var scalar = GetScalar(binding, value, severity);
                    if (!_values.TryGetValue(moniker, out var previous) || scalar != previous)
                        changes[moniker] = JsonConvert.SerializeObject(scalar);
                }
            }
            await CommitAsync(changes).ConfigureAwait(false);
            foreach (var (binding, value) in pending)
            {
                if (!value.Equals(ReadLegacy(binding, out _)))
                {
                    await WriteLegacyAsync(binding, value).ConfigureAwait(false);
                }
                else
                {
                    _legacyValues[binding.Moniker] = value;
                    _refresh(binding.Setting.Key, value);
                }
                _values[binding.Moniker] = GetScalar(binding, value, severity: false);
                _values[binding.SeverityMoniker] = GetScalar(binding, value, severity: true);
            }
        }, _reportRetryFailure, _cancellationToken, _retryDelay), _cancellationToken);

    public void OnNext(SettingsUpdate update)
    {
        // SubscribeAsync always delivers the initial snapshot first; initialization reads persisted values itself.
        if (Interlocked.Exchange(ref _initialSnapshot, 1) == 0)
            return;
        _observe(ApplyNativeUpdateAsync(update));
    }

    internal Task ApplyNativeUpdateAsync(SettingsUpdate update)
        => _gate.ExecuteAsync(async () =>
        {
            var results = update.Values.ToDictionary(result => result.Moniker, StringComparer.Ordinal);
            var pendingScalars = new List<(string moniker, string scalar)>(2);
            foreach (var binding in _bindings.Values)
            {
                pendingScalars.Clear();
                ICodeStyleOption2? merged = null;
                foreach (var severity in new[] { false, true })
                {
                    var moniker = severity ? binding.SeverityMoniker : binding.Moniker;
                    if (!results.TryGetValue(moniker, out var result))
                        continue;
                    var scalar = await GetUpdatedScalarAsync(result).ConfigureAwait(false);
                    var changed = !_values.TryGetValue(moniker, out var previous) || previous != scalar;
                    if (result.IsSuccess && result.MetaData.ChangeSource == s_source.Id)
                    {
                        _values[moniker] = scalar;
                        continue;
                    }

                    if (!changed && merged is null)
                        continue;

                    merged ??= ReadCurrentNativeValue(binding);
                    merged = (ICodeStyleOption2)binding.Setting.Update(merged, scalar, severity);
                    if (changed)
                        pendingScalars.Add((moniker, scalar));
                }
                if (merged is not null)
                {
                    await WriteLegacyAsync(binding, merged).ConfigureAwait(false);
                    foreach (var (moniker, scalar) in pendingScalars)
                        _values[moniker] = scalar;
                }
            }
        }, _cancellationToken);

    private ICodeStyleOption2 ReadCurrentNativeValue(Binding binding)
    {
        var current = ReadLegacy(binding, out _);
        if (_values.TryGetValue(binding.Moniker, out var preference))
            current = (ICodeStyleOption2)binding.Setting.Update(current, preference, severity: false);
        if (_values.TryGetValue(binding.SeverityMoniker, out var severity))
            current = (ICodeStyleOption2)binding.Setting.Update(current, severity, severity: true);
        return current;
    }

    private bool NativeValueMatches(Binding binding, ICodeStyleOption2 value)
        => _values.TryGetValue(binding.Moniker, out var preference) &&
           _values.TryGetValue(binding.SeverityMoniker, out var severity) &&
           preference == binding.Setting.SerializePreference(value) &&
           severity == CodeStyleSetting.SerializeSeverity(value.Notification);

    private async Task<string> GetUpdatedScalarAsync(ReadResult result)
    {
        if (result.IsSuccess)
            return JsonConvert.DeserializeObject<string>(result.Value!.Data)!;
        if (result.ResultCode != ReadResultCode.NotPersisted)
            throw new InvalidOperationException("Cannot read code style setting " + result.Moniker + ": " + result.ResultCode);

        var effective = await _native.GetValueAsync(result.Moniker, ReadOptions.NoRequirements, _cancellationToken).ConfigureAwait(false);
        if (!effective.IsSuccess)
            throw new InvalidOperationException("Cannot read effective code style setting " + result.Moniker + ": " + effective.ResultCode);
        return JsonConvert.DeserializeObject<string>(effective.Value!.Data)!;
    }

    public void OnError(Exception error) => _observe(Task.FromException(error));
    public void OnCompleted() { }

    public void Dispose()
    {
        _subscription?.Dispose();
        _legacySubset.SettingChangedAsync -= OnLegacyChangedAsync;
    }
}
