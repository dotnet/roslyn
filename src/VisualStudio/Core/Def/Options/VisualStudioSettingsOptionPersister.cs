// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Settings;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Options;

/// <summary>
/// Serializes settings to and from VS Settings storage.
/// </summary>
internal sealed class VisualStudioSettingsOptionPersister
{
    private readonly ISettingsManager _settingManager;
    private readonly Action<OptionKey2, object?> _refreshOption;
    private readonly ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> _readFallbacks;

    /// <summary>
    /// Storage keys that have been been fetched from <see cref="_settingManager"/>.
    /// We track this so if a later change happens, we know to refresh that value.
    /// </summary>
    private ImmutableDictionary<string, (OptionKey2 primaryOptionKey, string primaryStorageKey)> _storageKeysToMonitorForChanges
        = ImmutableDictionary<string, (OptionKey2, string)>.Empty;

    /// <remarks>
    /// We make sure this code is from the UI by asking for all <see cref="IOptionPersister"/> in <see cref="RoslynPackage.InitializeAsync"/>
    /// </remarks>
    public VisualStudioSettingsOptionPersister(Action<OptionKey2, object?> refreshOption, ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> readFallbacks, ISettingsManager settingsManager)
    {
        _settingManager = settingsManager;
        _refreshOption = refreshOption;
        _readFallbacks = readFallbacks;

        var settingsSubset = _settingManager.GetSubset("*");
        settingsSubset.SettingChangedAsync += OnSettingChangedAsync;
    }

    private Task OnSettingChangedAsync(object sender, PropertyChangedEventArgs args)
    {
        Contract.ThrowIfNull(_settingManager);

        if (_storageKeysToMonitorForChanges.TryGetValue(args.PropertyName, out var entry) &&
            TryFetch(entry.primaryOptionKey, entry.primaryStorageKey, out var newValue))
        {
            _refreshOption(entry.primaryOptionKey, newValue);
        }

        return Task.CompletedTask;
    }

    public bool TryFetch(OptionKey2 optionKey, string storageKey, out object? value)
    {
        var result = TryReadAndMonitorOptionValue(optionKey, storageKey, storageKey, optionKey.Option.Type, optionKey.Option.DefaultValue);
        if (result.HasValue)
        {
            value = result.Value;
            return true;
        }

        if (_readFallbacks.TryGetValue(optionKey.Option.Definition.ConfigName, out var lazyReadFallback))
        {
            var fallbackResult = lazyReadFallback.Value.TryRead(
                optionKey.Language,
                (altStorageKey, altStorageType, altDefaultValue) => TryReadAndMonitorOptionValue(optionKey, storageKey, altStorageKey, altStorageType, altDefaultValue));

            if (fallbackResult.HasValue)
            {
                value = fallbackResult.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    public Optional<object?> TryReadAndMonitorOptionValue(OptionKey2 primaryOptionKey, string primaryStorageKey, string storageKey, Type storageType, object? defaultValue)
    {
        Contract.ThrowIfNull(_settingManager);
        ImmutableInterlocked.GetOrAdd(ref _storageKeysToMonitorForChanges, storageKey, static (_, arg) => arg, factoryArgument: (primaryOptionKey, primaryStorageKey));
        return TryReadOptionValue(_settingManager, storageKey, storageType, defaultValue);
    }

    internal static Optional<object?> TryReadOptionValue(ISettingsManager manager, string storageKey, Type storageType, object? defaultValue)
    {
        if (storageType == typeof(bool))
            return Read<bool>();

        if (storageType == typeof(string))
            return Read<string>();

        if (storageType == typeof(int))
            return Read<int>();

        if (storageType.IsEnum)
            return manager.TryGetValue(storageKey, out int value) == GetValueResult.Success ? Enum.ToObject(storageType, value) : default(Optional<object?>);

        var underlyingType = Nullable.GetUnderlyingType(storageType);
        if (underlyingType?.IsEnum == true)
            return manager.TryGetValue(storageKey, out int? value) == GetValueResult.Success ? (value.HasValue ? Enum.ToObject(underlyingType, value.Value) : null) : default(Optional<object?>);

        if (storageType == typeof(NamingStylePreferences))
        {
            if (manager.TryGetValue(storageKey, out string value) == GetValueResult.Success)
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
            if (manager.TryGetValue(storageKey, out string value) == GetValueResult.Success)
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
            => manager.TryGetValue(storageKey, out T value) == GetValueResult.Success ? value : default(Optional<object?>);

        Optional<object?> ReadImmutableArray<T>()
            => manager.TryGetValue(storageKey, out T[] value) == GetValueResult.Success ? (value is null ? default : value.ToImmutableArray()) : default(Optional<object?>);
    }

    public Task PersistAsync(string storageKey, object? value)
    {
        Contract.ThrowIfNull(_settingManager);

        if (value is ICodeStyleOption codeStyleOption)
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
        else if (value != null)
        {
            var type = value.GetType();
            if (type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true)
            {
                value = (int)value;
            }
        }

        return _settingManager.SetValueAsync(storageKey, value, isMachineLocal: false);
    }
}
