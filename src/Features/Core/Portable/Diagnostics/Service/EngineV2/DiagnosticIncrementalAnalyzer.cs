// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    internal DiagnosticAnalyzerInfoCache DiagnosticAnalyzerInfoCache => _diagnosticAnalyzerRunner.AnalyzerInfoCache;

    public Task<ImmutableArray<DiagnosticAnalyzer>> GetAnalyzersForTestingPurposesOnlyAsync(Project project, CancellationToken cancellationToken)
        => _stateManager.GetOrCreateAnalyzersAsync(project.Solution.SolutionState, project.State, cancellationToken);

    private static string GetProjectLogMessage(Project project, ImmutableArray<DocumentDiagnosticAnalyzer> analyzers)
        => $"project: ({project.Id}), ({string.Join(Environment.NewLine, analyzers.Select(a => a.ToString()))})";
}
