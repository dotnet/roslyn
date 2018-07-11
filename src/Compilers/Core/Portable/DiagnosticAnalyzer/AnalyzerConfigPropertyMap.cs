// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public abstract class AnalyzerConfigPropertyMap
    {
        public abstract bool TryGetValue(string key, out string value);
    }

    internal sealed class CompilerAnalyzerConfigPropertyMap : AnalyzerConfigPropertyMap
    {
        public static CompilerAnalyzerConfigPropertyMap Empty { get; } = new CompilerAnalyzerConfigPropertyMap(
            ImmutableDictionary.Create<string, string>(CaseInsensitiveComparison.Comparer));

        private readonly ImmutableDictionary<string, string> _backing;

        public CompilerAnalyzerConfigPropertyMap(ImmutableDictionary<string, string> properties)
        {
            _backing = properties;
        }

        public string this[string key] => _backing[key];

        public IEnumerable<string> Keys => _backing.Keys;

        public IEnumerable<string> Values => _backing.Values;

        public int Count => _backing.Count;

        public bool ContainsKey(string key) => _backing.ContainsKey(key);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _backing.GetEnumerator();

        public override bool TryGetValue(string key, out string value) => _backing.TryGetValue(key, out value);

    }
}
