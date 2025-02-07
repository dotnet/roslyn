// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    /// <summary>
    /// this contains all states regarding a <see cref="DiagnosticAnalyzer"/>
    /// </summary>
    private sealed class StateSet
    {
        public readonly DiagnosticAnalyzer Analyzer;
        public readonly bool IsHostAnalyzer;

        public StateSet(DiagnosticAnalyzer analyzer, bool isHostAnalyzer)
        {
            Analyzer = analyzer;
            IsHostAnalyzer = isHostAnalyzer;
        }
    }
}
