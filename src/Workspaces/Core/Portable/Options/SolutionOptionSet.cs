// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Implements in-proc only storage for <see cref="Solution.Options"/>.
    /// Supports tracking changed options.
    /// Options that are not set in the option set are read from global options and cached.
    /// </summary>
    internal sealed class SolutionOptionSet : OptionSet
    {
        private readonly ILegacyGlobalOptionService _globalOptions;

        /// <summary>
        /// Cached values read from global options translated to public values.
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
            _globalOptions = globalOptions;
            _values = values;
            _changedOptionKeys = changedOptionKeys;
        }

        internal SolutionOptionSet(ILegacyGlobalOptionService globalOptions)
            : this(globalOptions, values: ImmutableDictionary<OptionKey, object?>.Empty, changedOptionKeys: ImmutableHashSet<OptionKey>.Empty)
        {
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowLocks = false)]
        private protected override object? GetOptionCore(OptionKey optionKey)
        {
            if (_values.TryGetValue(optionKey, out var value))
            {
                return value;
            }

            // Global options store internal representation of code style options. Translate to public representation.
            var internalValue = _globalOptions.GetOption(optionKey);
            value = internalValue is ICodeStyleOption codeStyleOption ? codeStyleOption.AsPublicCodeStyleOption() : internalValue;

            return ImmutableInterlocked.GetOrAdd(ref _values, optionKey, value);
        }

        public override OptionSet WithChangedOption(OptionKey optionKey, object? value)
        {
            // translate possibly internal value to public value:
            if (value is ICodeStyleOption codeStyleOption)
            {
                value = codeStyleOption.AsPublicCodeStyleOption();
            }

            // Make sure we first load this in current optionset
            var currentValue = GetOption(optionKey);

            // Check if the new value is the same as the current value.
            if (Equals(value, currentValue))
            {
                // Return a cloned option set as the public API 'WithChangedOption' guarantees a new option set is returned.
                return new SolutionOptionSet(_globalOptions, _values, _changedOptionKeys);
            }

            return new SolutionOptionSet(
                _globalOptions,
                _values.SetItem(optionKey, value),
                _changedOptionKeys.Add(optionKey));
        }

        /// <summary>
        /// Gets a list of all the options that were changed.
        /// </summary>
        internal IEnumerable<OptionKey> GetChangedOptionKeys()
            => _changedOptionKeys;

        internal (ImmutableArray<KeyValuePair<OptionKey2, object?>> internallyDefined, ImmutableArray<KeyValuePair<OptionKey, object?>> externallyDefined) GetChangedOptions()
        {
            var internallyDefined = _changedOptionKeys.Where(key => key.Option is IOption2).SelectAsArray(key => KeyValuePairUtil.Create(new OptionKey2((IOption2)key.Option, key.Language), GetOption(key)));
            var externallyDefined = _changedOptionKeys.Where(key => key.Option is not IOption2).SelectAsArray(key => KeyValuePairUtil.Create(key, GetOption(key)));
            return (internallyDefined, externallyDefined);
        }

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet? optionSet)
        {
            if (optionSet == this)
            {
                yield break;
            }

            foreach (var key in GetChangedOptionKeys())
            {
                var currentValue = optionSet?.GetOption(key);
                var changedValue = this.GetOption(key);
                if (!object.Equals(currentValue, changedValue))
                {
                    yield return key;
                }
            }
        }
    }
}
