// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

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
        public abstract bool TryGetValue(string key, [NotNullWhen(true)] out string? value);
    }

    internal sealed class CompilerAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        public static CompilerAnalyzerConfigOptions Empty { get; } = new CompilerAnalyzerConfigOptions(EmptyDictionary);

        private readonly ImmutableDictionary<string, string> _backing;

        public CompilerAnalyzerConfigOptions(ImmutableDictionary<string, string> properties)
        {
            _backing = properties;
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _backing.TryGetValue(key, out value);
    }
}
