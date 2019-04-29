// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public abstract class AnalyzerConfigOptions
    {
        /// <summary>
        /// Comparer that should be used for all analyzer config keys. This is a case-insensitive comparison based
        /// on Unicode case sensitivity rules for identifiers.
        /// </summary>
        public static StringComparer KeyComparer { get; } = AnalyzerConfig.Section.PropertiesKeyComparer;

        internal static ImmutableDictionary<string, string> EmptyDictionary = ImmutableDictionary.Create<string, string>(KeyComparer);

        /// <summary>
        /// Get an analyzer config value for the given key, using the <see cref="KeyComparer"/>.
        /// </summary>
        public abstract bool TryGetValue(string key, out string value);
    }

    internal sealed class CompilerAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        public static CompilerAnalyzerConfigOptions Empty { get; } = new CompilerAnalyzerConfigOptions(EmptyDictionary);

        private readonly ImmutableDictionary<string, string> _backing;

        public CompilerAnalyzerConfigOptions(ImmutableDictionary<string, string> properties)
        {
            _backing = properties;
        }

        public override bool TryGetValue(string key, out string value) => _backing.TryGetValue(key, out value);

    }
}
