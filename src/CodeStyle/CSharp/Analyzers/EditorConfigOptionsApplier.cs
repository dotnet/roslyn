// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal class EditorConfigOptionsApplier
    {
        public OptionSet ApplyConventions(OptionSet optionSet, ICodingConventionsSnapshot codingConventions)
        {
            return new CodingConventionsAnalyzerConfigOptions(codingConventions, optionSet);
        }

        private sealed class CodingConventionsAnalyzerConfigOptions : OptionSet
        {
            private readonly ICodingConventionsSnapshot _codingConventionsSnapshot;
            private readonly OptionSet _fallbackOptions;

            public CodingConventionsAnalyzerConfigOptions(ICodingConventionsSnapshot codingConventionsSnapshot, OptionSet fallbackOptions)
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
