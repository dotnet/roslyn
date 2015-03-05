// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerManager
    {
        private sealed class AnalyzerAndOptions
        {
            private readonly DiagnosticAnalyzer _analyzer;
            private readonly AnalyzerOptions _analyzerOptions;
            internal static readonly IEqualityComparer<AnalyzerAndOptions> Comparer = new AnalyzerAndOptionsComparer();

            public AnalyzerAndOptions(DiagnosticAnalyzer analyzer, AnalyzerOptions analyzerOptions)
            {
                Debug.Assert(analyzer != null);
                Debug.Assert(analyzerOptions != null);

                _analyzer = analyzer;
                _analyzerOptions = analyzerOptions;
            }

            public bool Equals(AnalyzerAndOptions other)
            {
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

            private sealed class AnalyzerAndOptionsComparer : IEqualityComparer<AnalyzerAndOptions>
            {
                public bool Equals(AnalyzerAndOptions x, AnalyzerAndOptions y)
                {
                    if (x == null)
                    {
                        return y == null;
                    }

                    return x.Equals(y);
                }

                public int GetHashCode(AnalyzerAndOptions obj)
                {
                    return obj != null ? obj.GetHashCode() : 0;
                }
            }
        }
    }
}