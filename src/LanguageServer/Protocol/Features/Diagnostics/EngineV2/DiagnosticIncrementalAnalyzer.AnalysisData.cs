// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// Data holder for all diagnostics for a project for an analyzer
        /// </summary>
        private readonly struct ProjectAnalysisData(
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result)
        {
            private readonly ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> Result = result;

            public bool TryGetResult(DiagnosticAnalyzer analyzer, out DiagnosticAnalysisResult result)
                => Result.TryGetValue(analyzer, out result);
        }
    }
}
