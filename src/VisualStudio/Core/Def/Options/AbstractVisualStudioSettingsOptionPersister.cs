// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Options;

/// <summary>
/// Serializes settings to and from VS Settings storage.
/// </summary>
internal abstract class AbstractVisualStudioSettingsOptionPersister<TSettingsManager>
{
    private readonly Action<OptionKey2, object?> _refreshOption;

    private ImmutableDictionary<string, (OptionKey2 primaryOptionKey, string primaryStorageKey)> _storageKeysToMonitorForChanges
        = ImmutableDictionary<string, (OptionKey2, string)>.Empty;

    public TSettingsManager SettingsManager { get; }

    protected AbstractVisualStudioSettingsOptionPersister(
        Action<OptionKey2, object?> refreshOption,
        TSettingsManager settingsManager)
    {
        _refreshOption = refreshOption;
        SettingsManager = settingsManager;
    }

    protected void RefreshIfTracked(string key)
    {
        if (_storageKeysToMonitorForChanges.TryGetValue(key, out var entry) &&
            TryFetch(entry.primaryOptionKey, entry.primaryStorageKey, out var newValue))
        {
            _refreshOption(entry.primaryOptionKey, newValue);
        }
    }

    public virtual bool TryFetch(OptionKey2 optionKey, string storageKey, out object? value)
        => TryFetchWorker(optionKey, storageKey, optionKey.Option.Type, out value);

    protected bool TryFetchWorker(OptionKey2 optionKey, string storageKey, Type optionType, out object? value)
    {
        var result = TryReadAndMonitorOptionValue(optionKey, storageKey, storageKey, optionType);
        if (result.HasValue)
        {
            value = result.Value;
            return true;
        }

        value = null;
        return false;
    }

    public Optional<object?> TryReadAndMonitorOptionValue(OptionKey2 primaryOptionKey, string primaryStorageKey, string storageKey, Type storageType)
    {
        ImmutableInterlocked.GetOrAdd(ref _storageKeysToMonitorForChanges, storageKey, static (_, arg) => arg, factoryArgument: (primaryOptionKey, primaryStorageKey));
        return TryReadOptionValue(primaryOptionKey, storageKey, storageType);
    }

    internal abstract Optional<object?> TryReadOptionValue(OptionKey2 optionKey, string storageKey, Type storageType);

    public abstract Task PersistAsync(OptionKey2 optionKey, string storageKey, object? value);
}
