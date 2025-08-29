// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    private sealed class ChecksumAndAnalyzersEqualityComparer
        : IEqualityComparer<(Checksum checksum, ImmutableArray<DiagnosticAnalyzer> analyzers)>
    {
        public static readonly ChecksumAndAnalyzersEqualityComparer Instance = new();

        public bool Equals((Checksum checksum, ImmutableArray<DiagnosticAnalyzer> analyzers) x, (Checksum checksum, ImmutableArray<DiagnosticAnalyzer> analyzers) y)
        {
            if (x.checksum != y.checksum)
                return false;

            // Fast path for when the analyzers are the same reference.
            return x.analyzers == y.analyzers || x.analyzers.SetEquals(y.analyzers);
        }

        public int GetHashCode((Checksum checksum, ImmutableArray<DiagnosticAnalyzer> analyzers) obj)
        {
            var hashCode = obj.checksum.GetHashCode();

            // Use addition so that we're resilient to any order for the analyzers.
            foreach (var analyzer in obj.analyzers)
                hashCode += analyzer.GetHashCode();

            return hashCode;
        }
    }
}
