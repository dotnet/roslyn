// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    internal sealed class OptionValueSet : OptionSet
    {
        public static readonly OptionValueSet Empty = new(ImmutableDictionary<OptionKey, object?>.Empty);

        private readonly ImmutableDictionary<OptionKey, object?> _values;

        public OptionValueSet(ImmutableDictionary<OptionKey, object?> values)
            => _values = values;

        private protected override object? GetOptionCore(OptionKey optionKey)
            => _values.TryGetValue(optionKey, out var value) ? value : optionKey.Option.DefaultValue;

        public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value)
            => new OptionValueSet(_values.SetItem(optionAndLanguage, value));

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
            => throw new NotSupportedException();
    }
}
