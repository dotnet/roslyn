// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
