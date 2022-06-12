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
    /// Serializable implementation of <see cref="OptionSet"/> for <see cref="Solution.Options"/>.
    /// It contains prepopulated fetched option values for all serializable options and values, and delegates to <see cref="WorkspaceOptionSet"/> for non-serializable values.
    /// It ensures a contract that values are immutable from this instance once observed.
    /// </summary>
    internal sealed partial class SerializableOptionSet : OptionSet
    {
        /// <summary>
        /// Fallback option set for non-serializable options. See comments on <see cref="WorkspaceOptionSet"/> for more details.
        /// </summary>
        private readonly WorkspaceOptionSet _workspaceOptionSet;

        /// <summary>
        /// Set of changed options in this option set which are non-serializable.
        /// </summary>
        private readonly ImmutableHashSet<OptionKey> _changedOptionKeysNonSerializable;

        private SerializableOptionSet(
            WorkspaceOptionSet workspaceOptionSet,
            ImmutableHashSet<OptionKey> changedOptionKeysNonSerializable)
        {
            _workspaceOptionSet = workspaceOptionSet;
            _changedOptionKeysNonSerializable = changedOptionKeysNonSerializable;
        }

        internal SerializableOptionSet(
            IOptionService optionService)
            : this(new WorkspaceOptionSet(optionService), changedOptionKeysNonSerializable: ImmutableHashSet<OptionKey>.Empty)
        {
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowLocks = false)]
        private protected override object? GetOptionCore(OptionKey optionKey)
        {
            return _workspaceOptionSet.GetOption(optionKey);
        }

        public override OptionSet WithChangedOption(OptionKey optionKey, object? value)
        {
            // Make sure we first load this in current optionset
            var currentValue = this.GetOption(optionKey);

            // Check if the new value is the same as the current value.
            if (Equals(value, currentValue))
            {
                // Return a cloned option set as the public API 'WithChangedOption' guarantees a new option set is returned.
                return new SerializableOptionSet(
                    _workspaceOptionSet, _changedOptionKeysNonSerializable);
            }

            return new SerializableOptionSet(
                (WorkspaceOptionSet)_workspaceOptionSet.WithChangedOption(optionKey, value),
                _changedOptionKeysNonSerializable.Add(optionKey));
        }

        /// <summary>
        /// Gets a list of all the options that were changed.
        /// </summary>
        internal IEnumerable<OptionKey> GetChangedOptions()
            => _changedOptionKeysNonSerializable;

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
