// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed partial class CompilerPerTreeOptionsProvider
    {
        internal class CompilerOptionSet : OptionSet, IEnumerable<KeyValuePair<Option<string>, string>>
        {
            private readonly ImmutableDictionary<Option<string>, string> _options;

            private CompilerOptionSet(ImmutableDictionary<Option<string>, string> options)
            {
                _options = options;
            }

            public CompilerOptionSet()
                : this(ImmutableDictionary.Create(
                    CompilerOptionSetKeyComparer.Instance,
                    StringOrdinalComparer.Instance))
            { }

            public override object GetOption(OptionKey optionKey)
                => _options.TryGetValue((Option<string>)optionKey.Option, out string value)
                    ? value
                    : optionKey.Option.DefaultValue;

            public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object value)
            {
                if (!StringOrdinalComparer.Equals(optionAndLanguage.Option.Feature, OptionFeatureName))
                {
                    throw new ArgumentException($"CompilerOptionSet only supports Feature {OptionFeatureName}");
                }

                return new CompilerOptionSet(_options.SetItem((Option<string>)optionAndLanguage.Option, (string)value));
            }

            public IEnumerator<KeyValuePair<Option<string>, string>> GetEnumerator() => _options.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            internal class CompilerOptionSetKeyComparer : IEqualityComparer<Option<string>>
            {
                public static readonly CompilerOptionSetKeyComparer Instance = new CompilerOptionSetKeyComparer();

                private CompilerOptionSetKeyComparer() { }

                public bool Equals(Option<string> x, Option<string> y)
                    => CaseInsensitiveComparison.Equals(x.Name, y.Name);

                public int GetHashCode(Option<string> obj)
                    => CaseInsensitiveComparison.GetHashCode(obj.Name);
            }
        }
    }
}
