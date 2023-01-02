// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

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

        internal override object? GetInternalOptionValue(OptionKey optionKey)
            => _values.TryGetValue(optionKey, out var value) ? value : optionKey.Option.DefaultValue;

        internal override OptionSet WithChangedOptionInternal(OptionKey optionKey, object? internalValue)
            => new TestOptionSet(_values.SetItem(optionKey, internalValue));
    }
}
