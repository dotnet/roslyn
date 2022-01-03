// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class AnalyzerConfigOptionsDictionary : AnalyzerConfigOptions
    {
        internal static ImmutableDictionary<string, string> EmptyDictionary = ImmutableDictionary.Create<string, string>(KeyComparer);

        public static AnalyzerConfigOptionsDictionary Empty { get; } = new AnalyzerConfigOptionsDictionary(EmptyDictionary);

        private readonly ImmutableDictionary<string, string> _options;

        public AnalyzerConfigOptionsDictionary(ImmutableDictionary<string, string> options)
            => _options = options;

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            => _options.TryGetValue(key, out value);
    }
}
