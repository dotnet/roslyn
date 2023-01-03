// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
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
        private readonly ILegacyWorkspaceOptionService _globalOptions;

        /// <summary>
        /// Cached values read from global options.
        /// </summary>
        private ImmutableDictionary<OptionKey, object?> _values;

        /// <summary>
        /// Keys of options whose current value stored in <see cref="_values"/> differs from the value originally read from global options.
        /// </summary>
        private readonly ImmutableHashSet<OptionKey> _changedOptionKeys;

        private SolutionOptionSet(
            ILegacyWorkspaceOptionService globalOptions,
            ImmutableDictionary<OptionKey, object?> values,
            ImmutableHashSet<OptionKey> changedOptionKeys)
        {
            _globalOptions = globalOptions;
            _values = values;
            _changedOptionKeys = changedOptionKeys;
        }

        internal SolutionOptionSet(ILegacyWorkspaceOptionService globalOptions)
            : this(globalOptions, values: ImmutableDictionary<OptionKey, object?>.Empty, changedOptionKeys: ImmutableHashSet<OptionKey>.Empty)
        {
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowLocks = false)]
        private protected override object? GetOptionCore(OptionKey optionKey)
        {
            if (_values.TryGetValue(optionKey, out var value))
            {
                return value is ICodeStyleOption codeStyleOption ? codeStyleOption.AsPublicCodeStyleOption() : value;
            }

            value = _globalOptions.GetOption(optionKey);
            return ImmutableInterlocked.GetOrAdd(ref _values, optionKey, value);
        }

        public override OptionSet WithChangedOption(OptionKey optionKey, object? value)
        {
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
        internal IEnumerable<OptionKey> GetChangedOptions()
            => _changedOptionKeys;

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet? optionSet)
        {
            if (optionSet == this)
            {
                yield break;
            }

            foreach (var key in GetChangedOptions())
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
