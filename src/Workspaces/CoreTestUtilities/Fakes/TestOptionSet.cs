// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal class TestOptionSet : OptionSet
    {
        public static new readonly TestOptionSet Empty = new(ImmutableDictionary<OptionKey, object?>.Empty);

        private readonly ImmutableDictionary<OptionKey, object?> _values;

        public TestOptionSet(ImmutableDictionary<OptionKey, object?> values)
        {
            _values = values;
        }

        private protected override object? GetOptionCore(OptionKey optionKey)
            => _values.TryGetValue(optionKey, out var value) ? value : optionKey.Option.DefaultValue;

        public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value)
        {
            return new TestOptionSet(_values.SetItem(optionAndLanguage, value));
        }

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
        {
            foreach (var (key, value) in _values)
            {
                var currentValue = optionSet.GetOption(key);
                if (!object.Equals(currentValue, value))
                    yield return key;
            }
        }
    }
}
