// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed partial class AnalyzerConfigOptionSet
    {
        private sealed class AnalyzerConfigOptionsImpl : AnalyzerConfigOptions
        {
            private readonly AnalyzerConfigOptions _options;
            private readonly AnalyzerConfigOptions _fallbackOptions;

            public AnalyzerConfigOptionsImpl(AnalyzerConfigOptions options, AnalyzerConfigOptions fallbackOptions)
            {
                _options = options;
                _fallbackOptions = fallbackOptions;
            }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                if (_options.TryGetValue(key, out value))
                {
                    return true;
                }

                return _fallbackOptions.TryGetValue(key, out value);
            }
        }
    }
}
