// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

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

    protected abstract bool TryGetValue<T>(OptionKey2 optionKey, string storageKey, Type storageType, out T value);
    protected abstract Task SetValueAsync(OptionKey2 optionKey, string storageKey, object? value, bool isMachineLocal);

    protected void RefreshIfTracked(string key)
    {
        if (_storageKeysToMonitorForChanges.TryGetValue(key, out var entry) &&
            TryFetch(entry.primaryOptionKey, entry.primaryStorageKey, out var newValue))
        {
            _refreshOption(entry.primaryOptionKey, newValue);
        }
    }

    public virtual bool TryFetch(OptionKey2 optionKey, string storageKey, out object? value)
    {
        var result = TryReadAndMonitorOptionValue(optionKey, storageKey, storageKey, optionKey.Option.Type, optionKey.Option.DefaultValue);
        if (result.HasValue)
        {
            value = result.Value;
            return true;
        }

        value = null;
        return false;
    }

    protected Optional<object?> TryReadAndMonitorOptionValue(OptionKey2 primaryOptionKey, string primaryStorageKey, string storageKey, Type storageType, object? defaultValue)
    {
        ImmutableInterlocked.GetOrAdd(ref _storageKeysToMonitorForChanges, storageKey, static (_, arg) => arg, factoryArgument: (primaryOptionKey, primaryStorageKey));
        return TryReadOptionValue(primaryOptionKey, storageKey, storageType, defaultValue);
    }

    internal Optional<object?> TryReadOptionValue(OptionKey2 optionKey, string storageKey, Type storageType, object? defaultValue)
    {
        if (storageType == typeof(bool))
            return Read<bool>();

        if (storageType == typeof(string))
            return Read<string>();

        if (storageType == typeof(int))
            return Read<int>();

        // Encoding of enums is handled by the underlying settings store.  For example, with the legacy settings manager
        // we encode them as ints.  With the unified settings manager we encode them as the name of the enum member.
        if (storageType.IsEnum || Nullable.GetUnderlyingType(storageType)?.IsEnum is true)
            return Read<object>();

        if (storageType == typeof(NamingStylePreferences))
        {
            if (TryGetValue(optionKey, storageKey, storageType, out string value))
            {
                try
                {
                    return NamingStylePreferences.FromXElement(XElement.Parse(value));
                }
                catch
                {
                    return default;
                }
            }

            return default;
        }

        if (defaultValue is ICodeStyleOption2 codeStyle)
        {
            if (TryGetValue(optionKey, storageKey, storageType, out string value))
            {
                try
                {
                    return new Optional<object?>(codeStyle.FromXElement(XElement.Parse(value)));
                }
                catch
                {
                    return default;
                }
            }

            return default;
        }

        if (storageType == typeof(long))
            return Read<long>();

        if (storageType == typeof(bool?))
            return Read<bool?>();

        if (storageType == typeof(int?))
            return Read<int?>();

        if (storageType == typeof(long?))
            return Read<long?>();

        if (storageType == typeof(ImmutableArray<bool>))
            return ReadImmutableArray<bool>();

        if (storageType == typeof(ImmutableArray<string>))
            return ReadImmutableArray<string>();

        if (storageType == typeof(ImmutableArray<int>))
            return ReadImmutableArray<int>();

        if (storageType == typeof(ImmutableArray<long>))
            return ReadImmutableArray<long>();

        throw ExceptionUtilities.UnexpectedValue(storageType);

        Optional<object?> Read<T>()
            => TryGetValue(optionKey, storageKey, storageType, out T value) ? value : default(Optional<object?>);

        Optional<object?> ReadImmutableArray<T>()
            => TryGetValue(optionKey, storageKey, storageType, out T[] value) ? (value is null ? default : value.ToImmutableArray()) : default(Optional<object?>);
    }

    public Task PersistAsync(OptionKey2 optionKey, string storageKey, object? value)
    {
        if (value is ICodeStyleOption2 codeStyleOption)
        {
            // We store these as strings, so serialize
            value = codeStyleOption.ToXElement().ToString();
        }
        else if (value is NamingStylePreferences namingStyle)
        {
            // We store these as strings, so serialize
            value = namingStyle.CreateXElement().ToString();
        }
        else if (value is ImmutableArray<string> stringArray)
        {
            value = stringArray.IsDefault ? null : stringArray.ToArray();
        }
        else if (value is ImmutableArray<bool> boolArray)
        {
            value = boolArray.IsDefault ? null : boolArray.ToArray();
        }
        else if (value is ImmutableArray<int> intArray)
        {
            value = intArray.IsDefault ? null : intArray.ToArray();
        }
        else if (value is ImmutableArray<long> longArray)
        {
            value = longArray.IsDefault ? null : longArray.ToArray();
        }

        return SetValueAsync(optionKey, storageKey, value, isMachineLocal: false);
    }
}
