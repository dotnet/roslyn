// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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

            public override bool TryGetValue(string key, out string value)
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
