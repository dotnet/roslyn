// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerManager
    {
        private sealed class AnalyzerAndOptions
        {
            private readonly DiagnosticAnalyzer _analyzer;
            private readonly AnalyzerOptions _analyzerOptions;

            public AnalyzerAndOptions(DiagnosticAnalyzer analyzer, AnalyzerOptions analyzerOptions)
            {
                Debug.Assert(analyzer != null);
                Debug.Assert(analyzerOptions != null);

                _analyzer = analyzer;
                _analyzerOptions = analyzerOptions;
            }

            public bool Equals(AnalyzerAndOptions other)
            {
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return other != null &&
                    _analyzer.Equals(other._analyzer) &&
                    _analyzerOptions.Equals(other._analyzerOptions);
            }

            public override bool Equals(object other)
            {
                return Equals(other as AnalyzerAndOptions);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_analyzer.GetHashCode(), _analyzerOptions.GetHashCode());
            }
        }
    }
}