// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private static Task<CompilationWithAnalyzers?> CreateCompilationWithAnalyzersAsync(Project project, IdeAnalyzerOptions ideOptions, IEnumerable<StateSet> stateSets, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            => DocumentAnalysisExecutor.CreateCompilationWithAnalyzersAsync(project, ideOptions, stateSets.Select(s => s.Analyzer), includeSuppressedDiagnostics, cancellationToken);
    }
}
