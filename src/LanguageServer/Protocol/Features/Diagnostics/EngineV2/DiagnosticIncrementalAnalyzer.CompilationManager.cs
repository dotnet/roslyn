// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2;

internal partial class DiagnosticIncrementalAnalyzer
{
    private static Task<CompilationWithAnalyzersPair?> CreateCompilationWithAnalyzersAsync(Project project, ImmutableArray<StateSet> stateSets, bool crashOnAnalyzerException, CancellationToken cancellationToken)
        => DocumentAnalysisExecutor.CreateCompilationWithAnalyzersAsync(project, stateSets.SelectAsArray(s => !s.IsHostAnalyzer, s => s.Analyzer), stateSets.SelectAsArray(s => s.IsHostAnalyzer, s => s.Analyzer), crashOnAnalyzerException, cancellationToken);
}
