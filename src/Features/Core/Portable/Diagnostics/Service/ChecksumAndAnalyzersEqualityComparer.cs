// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    private sealed class AnalyzersEqualityComparer
        : IEqualityComparer<ImmutableArray<DiagnosticAnalyzer>>
    {
        public static readonly AnalyzersEqualityComparer Instance = new();

        public bool Equals(ImmutableArray<DiagnosticAnalyzer> x, ImmutableArray<DiagnosticAnalyzer> y)
        {
            // Fast path for when the analyzers are the same reference.
            return x == y || x.SetEquals(y);
        }

        public int GetHashCode(ImmutableArray<DiagnosticAnalyzer> obj)
        {
            var hashCode = 0;

            // Use addition so that we're resilient to any order for the analyzers.
            foreach (var analyzer in obj)
                hashCode += analyzer.GetHashCode();

            return hashCode;
        }
    }
}
