// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Settings;

namespace Microsoft.VisualStudio.LanguageServices.Options;

/// <summary>
/// Serializes settings to and from VS Settings storage.
/// </summary>
internal sealed class VisualStudioSettingsOptionPersister : AbstractVisualStudioSettingsOptionPersister<ISettingsManager>
{
    private readonly ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> _readFallbacks;

    public VisualStudioSettingsOptionPersister(
        Action<OptionKey2, object?> refreshOption,
        ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> readFallbacks,
        ISettingsManager settingsManager)
        : base(refreshOption, settingsManager)
    {
        _readFallbacks = readFallbacks;

        var settingsSubset = settingsManager.GetSubset("*");
        settingsSubset.SettingChangedAsync += OnSettingChangedAsync;
    }

    private Task OnSettingChangedAsync(object sender, PropertyChangedEventArgs args)
    {
        Contract.ThrowIfNull(this.SettingsManager);

        RefreshIfTracked(args.PropertyName);
        return Task.CompletedTask;
    }

    public override bool TryFetch(OptionKey2 optionKey, string storageKey, out object? value)
    {
        if (base.TryFetch(optionKey, storageKey, out value))
            return true;

        if (_readFallbacks.TryGetValue(optionKey.Option.Definition.ConfigName, out var lazyReadFallback))
        {
            var fallbackResult = lazyReadFallback.Value.TryRead(
                optionKey.Language,
                (altStorageKey, altStorageType) => TryReadAndMonitorOptionValue(optionKey, storageKey, altStorageKey, altStorageType));

            if (fallbackResult.HasValue)
            {
                value = fallbackResult.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private bool TryGetValue<T>(string storageKey, out T value)
        => this.SettingsManager.TryGetValue(storageKey, out value) == GetValueResult.Success;

    private Task SetValueAsync(string storageKey, object? value)
        => this.SettingsManager.SetValueAsync(storageKey, value, isMachineLocal: false);

    internal override Optional<object?> TryReadOptionValue(OptionKey2 optionKey, string storageKey, Type storageType)
    {
        if (storageType == typeof(bool))
            return Read<bool>();

        if (storageType == typeof(string))
            return Read<string>();

        if (storageType == typeof(int))
            return Read<int>();

        if (storageType.IsEnum)
            return TryGetValue(storageKey, out int value) ? Enum.ToObject(storageType, value) : default(Optional<object?>);

        var underlyingType = Nullable.GetUnderlyingType(storageType);
        if (underlyingType?.IsEnum == true)
        {
            if (TryGetValue(storageKey, out int? nullableValue))
            {
                return nullableValue.HasValue ? Enum.ToObject(underlyingType, nullableValue.Value) : null;
            }
            else if (TryGetValue(storageKey, out int value))
            {
                return Enum.ToObject(underlyingType, value);
            }
            else
            {
                return default;
            }
        }

        if (storageType == typeof(NamingStylePreferences))
        {
            if (TryGetValue(storageKey, out string value))
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

        if (typeof(ICodeStyleOption2).IsAssignableFrom(storageType))
        {
            if (TryGetValue(storageKey, out string value))
            {
                try
                {
                    var fromXElementMember = storageType.GetMethod(nameof(CodeStyleOption2<>.FromXElement), BindingFlags.Public | BindingFlags.Static);
                    return new Optional<object?>(fromXElementMember.Invoke(null, [XElement.Parse(value)]));
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
            => TryGetValue(storageKey, out T value) ? value : default(Optional<object?>);

        Optional<object?> ReadImmutableArray<T>()
            => TryGetValue(storageKey, out T[] value) ? (value is null ? default : value.ToImmutableArray()) : default(Optional<object?>);
    }

    public override Task PersistAsync(OptionKey2 optionKey, string storageKey, object? value)
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
        else if (value != null)
        {
            var type = value.GetType();
            if (type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true)
            {
                value = (int)value;
            }
        }

        return SetValueAsync(storageKey, value);
    }
}
