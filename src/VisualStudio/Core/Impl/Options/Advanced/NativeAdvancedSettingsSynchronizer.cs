// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.RpcContracts.UnifiedSettings;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

internal sealed class NativeAdvancedSettingsSynchronizer : IObserver<SettingsUpdate>, IDisposable
{
    // Brokered NoFallbackToDefault distinguishes absence from a registered default.
    private const ReadOptions PersistedRead = ReadOptions.NoFallbackToDefault | ReadOptions.RequireValidation | ReadOptions.RequireRegistration;
    private static readonly ChangeSource s_source = new(nameof(NativeAdvancedSettingsSynchronizer), new Guid("31fb6817-5455-4ef5-a7db-e55d35e87db4"));
    private readonly IUnifiedSettingsService _native;
    private readonly Dictionary<string, Binding> _bindings;
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);
    private readonly ReentrantSemaphore _gate;
    private readonly Action<OptionKey2, object?> _refresh;
    private readonly Action<Task> _observe;
    private readonly CancellationToken _cancellationToken;
    private IDisposable? _subscription;
    private int _initialSnapshot;

    internal sealed record Binding(OptionKey2 Key, string Moniker, Func<Optional<object?>> ReadLegacy, Func<object?, Task> WriteLegacy)
    {
        public object? Default => Key.Option.DefaultValue;

        public string Serialize(object? value)
            => JsonConvert.SerializeObject((bool)value!);

        public object? Deserialize(string json)
        {
            var token = JToken.Parse(json);
            if (token.Type == JTokenType.Boolean)
                return token.Value<bool>();
            throw new InvalidOperationException("Invalid Advanced setting: " + Moniker);
        }
    }

    public NativeAdvancedSettingsSynchronizer(IUnifiedSettingsService native, IEnumerable<Binding> bindings,
        Action<OptionKey2, object?> refresh, Action<Task> observe, JoinableTaskContext context, CancellationToken cancellationToken)
    {
        _native = native;
        _bindings = bindings.ToDictionary(binding => binding.Moniker, StringComparer.Ordinal);
        _refresh = refresh;
        _observe = observe;
        _gate = ReentrantSemaphore.Create(1, context, ReentrantSemaphore.ReentrancyMode.NotRecognized);
        _cancellationToken = cancellationToken;
    }

    internal Task InitializeAsync()
        => _gate.ExecuteAsync(async () =>
        {
            _subscription = await _native.SubscribeAsync(this, _bindings.Keys.ToArray(), PersistedRead, _cancellationToken).ConfigureAwait(false);
            foreach (var binding in _bindings.Values)
            {
                var legacy = binding.ReadLegacy();
                var current = await ReadNativeAsync(binding).ConfigureAwait(false);
                var value = current.HasValue ? current.Value : legacy.HasValue ? legacy.Value : binding.Default;
                _values[binding.Moniker] = value;
                if (current.HasValue)
                {
                    if (!Equals(value, legacy.HasValue ? legacy.Value : binding.Default))
                        await binding.WriteLegacy(value).ConfigureAwait(false);
                }
                else if (legacy.HasValue)
                {
                    // A native import that arrives during bootstrap must win over the legacy snapshot.
                    current = await ReadNativeAsync(binding).ConfigureAwait(false);
                    if (current.HasValue)
                    {
                        value = current.Value;
                        _values[binding.Moniker] = value;
                        await binding.WriteLegacy(value).ConfigureAwait(false);
                    }
                    else
                    {
                        legacy = binding.ReadLegacy();
                        value = legacy.HasValue ? legacy.Value : binding.Default;
                        _values[binding.Moniker] = value;
                        if (legacy.HasValue)
                            await CommitAsync(binding, value).ConfigureAwait(false);
                    }
                }
                _refresh(binding.Key, value);
            }
        }, _cancellationToken);

    private async Task<Optional<object?>> ReadNativeAsync(Binding binding)
    {
        var result = await _native.GetValueAsync(binding.Moniker, PersistedRead, _cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
            return new(binding.Deserialize(result.Value!.Data));
        if (result.ResultCode == ReadResultCode.NotPersisted)
            return default;
        throw new InvalidOperationException("Cannot read Advanced setting " + binding.Moniker + ": " + result.ResultCode);
    }

    private async Task CommitAsync(Binding binding, object? value)
    {
        var changes = new Dictionary<string, string> { [binding.Moniker] = binding.Serialize(value) };
        var result = await _native.RequestCommitAsync(s_source, new SettingsCommit(changes, nameof(NativeAdvancedSettingsSynchronizer)), _cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            throw new InvalidOperationException("Advanced settings commit failed: " + result.ResultCode);
    }

    internal void OnRegistryChanged()
        => _observe(ApplyLegacyChangesAsync(_bindings.Values));

    private Task ApplyLegacyChangesAsync(IEnumerable<Binding> bindings)
        => _gate.ExecuteAsync(async () =>
        {
            foreach (var binding in bindings)
            {
                var legacy = binding.ReadLegacy();
                var value = legacy.HasValue ? legacy.Value : binding.Default;
                if (_values.TryGetValue(binding.Moniker, out var previous) && Equals(previous, value))
                    continue;
                _values[binding.Moniker] = value;
                try
                {
                    await CommitAsync(binding, value).ConfigureAwait(false);
                }
                catch
                {
                    _values[binding.Moniker] = previous;
                    throw;
                }
                _refresh(binding.Key, value);
            }
        }, _cancellationToken);

    public void OnNext(SettingsUpdate update)
    {
        if (Interlocked.Exchange(ref _initialSnapshot, 1) == 0)
            return;
        _observe(ApplyNativeUpdateAsync(update));
    }

    internal Task ApplyNativeUpdateAsync(SettingsUpdate update)
        => _gate.ExecuteAsync(async () =>
        {
            foreach (var moniker in update.Values.Select(result => result.Moniker).Distinct())
            {
                if (!_bindings.TryGetValue(moniker, out var binding))
                    continue;
                // Read current data rather than replaying a queued snapshot superseded by a newer edit.
                var current = await ReadNativeAsync(binding).ConfigureAwait(false);
                var value = current.HasValue ? current.Value : binding.Default;
                if (_values.TryGetValue(moniker, out var previous) && Equals(previous, value))
                    continue;
                await binding.WriteLegacy(value).ConfigureAwait(false);
                _values[moniker] = value;
                _refresh(binding.Key, value);
            }
        }, _cancellationToken);

    public void OnError(Exception error) => _observe(Task.FromException(error));
    public void OnCompleted() { }

    public void Dispose()
    {
        _subscription?.Dispose();
        _gate.Dispose();
    }
}
