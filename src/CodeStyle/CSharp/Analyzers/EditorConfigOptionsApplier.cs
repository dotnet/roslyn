// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal class EditorConfigOptionsApplier
    {
        public AnalyzerConfigOptions ApplyConventions(AnalyzerConfigOptions optionSet, ICodingConventionsSnapshot codingConventions)
        {
            return new CodingConventionsAnalyzerConfigOptions(codingConventions, optionSet);
        }

        private sealed class CodingConventionsAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly ICodingConventionsSnapshot _codingConventionsSnapshot;
            private readonly AnalyzerConfigOptions _fallbackOptions;

            public CodingConventionsAnalyzerConfigOptions(ICodingConventionsSnapshot codingConventionsSnapshot, AnalyzerConfigOptions fallbackOptions)
            {
                _codingConventionsSnapshot = codingConventionsSnapshot;
                _fallbackOptions = fallbackOptions;
            }

            public override bool TryGetValue(string key, out string value)
            {
                if (_codingConventionsSnapshot.TryGetConventionValue(key, out value))
                {
                    return true;
                }

                return _fallbackOptions.TryGetValue(key, out value);
            }
        }
    }
}
