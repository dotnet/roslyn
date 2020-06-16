// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.Next.UnitTests.Mocks
{
    internal class TestOptionSet : OptionSet
    {
        private readonly ImmutableDictionary<OptionKey, object> _values;

        public TestOptionSet()
        {
            _values = ImmutableDictionary<OptionKey, object>.Empty;
        }

        private TestOptionSet(ImmutableDictionary<OptionKey, object> values)
        {
            _values = values;
        }

        private protected override object GetOptionCore(OptionKey optionKey)
        {
            Contract.ThrowIfFalse(_values.TryGetValue(optionKey, out var value));

            return value;
        }

        public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object value)
        {
            return new TestOptionSet(_values.SetItem(optionAndLanguage, value));
        }

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
        {
            foreach (var kvp in _values)
            {
                var currentValue = optionSet.GetOption(kvp.Key);
                if (!object.Equals(currentValue, kvp.Value))
                {
                    yield return kvp.Key;
                }
            }
        }
    }
}
