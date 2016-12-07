// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public override object GetOption(OptionKey optionKey)
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
