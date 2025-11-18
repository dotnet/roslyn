// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Implements in-proc only storage for <see cref="Solution.Options"/>.
/// Supports tracking changed options.
/// Options that are not set in the option set are read from global options and cached.
/// </summary>
internal sealed class SolutionOptionSet : OptionSet
{
    private readonly ILegacyGlobalOptionService _legacyGlobalOptions;

    /// <summary>
    /// Cached values read from global options. Stores internal values of options.
    /// </summary>
    private ImmutableDictionary<OptionKey, object?> _values;

    /// <summary>
    /// Keys of options whose current value stored in <see cref="_values"/> differs from the value originally read from global options.
    /// </summary>
    private readonly ImmutableHashSet<OptionKey> _changedOptionKeys;

    private SolutionOptionSet(
        ILegacyGlobalOptionService globalOptions,
        ImmutableDictionary<OptionKey, object?> values,
        ImmutableHashSet<OptionKey> changedOptionKeys)
    {
        _legacyGlobalOptions = globalOptions;
        _values = values;
        _changedOptionKeys = changedOptionKeys;
    }

    internal SolutionOptionSet(ILegacyGlobalOptionService globalOptions)
        : this(globalOptions, values: ImmutableDictionary<OptionKey, object?>.Empty, changedOptionKeys: [])
    {
    }

    [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowLocks = false)]
    internal override object? GetInternalOptionValue(OptionKey optionKey)
    {
        if (_values.TryGetValue(optionKey, out var value))
        {
            return value;
        }

        value = (optionKey.Option is IOption2 internallyDefinedOption)
            ? _legacyGlobalOptions.GlobalOptions.GetOption<object?>(new OptionKey2(internallyDefinedOption, optionKey.Language))
            : _legacyGlobalOptions.GetExternallyDefinedOption(optionKey);

        return ImmutableInterlocked.GetOrAdd(ref _values, optionKey, value);
    }

    internal override OptionSet WithChangedOptionInternal(OptionKey optionKey, object? internalValue)
    {
        // Make sure we first load this in current optionset
        var currentInternalValue = GetInternalOptionValue(optionKey);

        // Check if the new value is the same as the current value.
        if (Equals(internalValue, currentInternalValue))
        {
            // Return a cloned option set as the public API 'WithChangedOption' guarantees a new option set is returned.
            return new SolutionOptionSet(_legacyGlobalOptions, _values, _changedOptionKeys);
        }

        return new SolutionOptionSet(
            _legacyGlobalOptions,
            _values.SetItem(optionKey, internalValue),
            _changedOptionKeys.Add(optionKey));
    }

    internal (ImmutableArray<KeyValuePair<OptionKey2, object?>> internallyDefined, ImmutableArray<KeyValuePair<OptionKey, object?>> externallyDefined) GetChangedOptions()
    {
        var internallyDefined = _changedOptionKeys.SelectAsArray(
            predicate: key => key.Option is IOption2,
            selector: key => KeyValuePair.Create(new OptionKey2((IOption2)key.Option, key.Language), _values[key]));
        var externallyDefined = _changedOptionKeys.SelectAsArray(
            predicate: key => key.Option is not IOption2,
            selector: key => KeyValuePair.Create(key, _values[key]));
        return (internallyDefined, externallyDefined);
    }
}
