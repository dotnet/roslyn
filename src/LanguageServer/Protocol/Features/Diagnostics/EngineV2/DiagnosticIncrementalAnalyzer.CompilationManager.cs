// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        private static Task<CompilationWithAnalyzersPair?> CreateCompilationWithAnalyzersAsync(
            Project project,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            HostAnalyzerInfo hostAnalyzerInfo,
            bool crashOnAnalyzerException, CancellationToken cancellationToken)
            => DocumentAnalysisExecutor.CreateCompilationWithAnalyzersAsync(
                project,
                analyzers.WhereAsArray(static (a, info) => !info.IsHostAnalyzer(a), hostAnalyzerInfo),
                analyzers.WhereAsArray(static (a, info) => info.IsHostAnalyzer(a), hostAnalyzerInfo),
                crashOnAnalyzerException,
                cancellationToken);
    }
}
