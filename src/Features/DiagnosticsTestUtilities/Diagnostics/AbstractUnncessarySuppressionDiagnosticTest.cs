// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;

public abstract class AbstractUnnecessarySuppressionDiagnosticTest(ITestOutputHelper logger)
    : AbstractUserDiagnosticTest_NoEditor<
        TestHostDocument,
        TestHostProject,
        TestHostSolution,
        TestWorkspace>(logger)
{
    internal abstract CodeFixProvider CodeFixProvider { get; }
    internal abstract AbstractRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer SuppressionAnalyzer { get; }
    internal abstract ImmutableArray<DiagnosticAnalyzer> OtherAnalyzers { get; }

    private void AddAnalyzersToWorkspace(TestWorkspace workspace)
    {
        var analyzerReference = new AnalyzerImageReference(OtherAnalyzers.Add(SuppressionAnalyzer));
        workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences([analyzerReference]));
    }

    internal override async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(
        TestWorkspace workspace, TestParameters parameters)
    {
        AddAnalyzersToWorkspace(workspace);
        var document = GetDocumentAndSelectSpan(workspace, out var span);
        return await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, document, span);
    }

    internal override async Task<(ImmutableArray<Diagnostic>, ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetDiagnosticAndFixesAsync(
        TestWorkspace workspace, TestParameters parameters)
    {
        AddAnalyzersToWorkspace(workspace);

        var (document, span, annotation) = await GetDocumentAndSelectSpanOrAnnotatedSpan(workspace);

        // Include suppressed diagnostics as they are needed by unnecessary suppressions analyzer.
        var testDriver = new TestDiagnosticAnalyzerDriver(workspace, includeSuppressedDiagnostics: true);
        var diagnostics = await testDriver.GetAllDiagnosticsAsync(document, span);

        // Filter out suppressed diagnostics before invoking code fix.
        diagnostics = diagnostics.Where(d => !d.IsSuppressed);

        return await GetDiagnosticAndFixesAsync(
            diagnostics, CodeFixProvider, testDriver, document,
            span, annotation, parameters.index);
    }
}
