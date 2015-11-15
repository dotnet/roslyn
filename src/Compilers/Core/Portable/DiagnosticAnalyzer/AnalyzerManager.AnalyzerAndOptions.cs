// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerManager
    {
        private sealed class AnalyzerAndOptions
        {
            public readonly DiagnosticAnalyzer Analyzer;
            private readonly AnalyzerOptions _analyzerOptions;

            public AnalyzerAndOptions(DiagnosticAnalyzer analyzer, AnalyzerOptions analyzerOptions)
            {
                Debug.Assert(analyzer != null);
                Debug.Assert(analyzerOptions != null);

                Analyzer = analyzer;
                // _analyzerOptions is used only as a key for equality testing, and only the properties
                // declared by AnalyzerOptions are appropriate for use in the comparison. (For example,
                // two AnalyzersAndOptions that differ only by Workspace ought to be treated as equal.)
                // If the supplied analyzerOptions is an instance of a derived type, holding on to that object
                // can introduce a memory leak, as well as causing false negatives in comparison. Avoid these
                // issues by creating a new object with only the desired properties. 
                _analyzerOptions = new AnalyzerOptions(analyzerOptions.AdditionalFiles);
            }

            public bool Equals(AnalyzerAndOptions other)
            {
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return other != null &&
                    Analyzer.Equals(other.Analyzer) &&
                    _analyzerOptions.Equals(other._analyzerOptions);
            }

            public override bool Equals(object other)
            {
                return Equals(other as AnalyzerAndOptions);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(Analyzer.GetHashCode(), _analyzerOptions.GetHashCode());
            }
        }
    }
}