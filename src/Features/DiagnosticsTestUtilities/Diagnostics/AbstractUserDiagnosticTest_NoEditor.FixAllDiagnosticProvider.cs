// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public abstract partial class AbstractUserDiagnosticTest_NoEditor
    {
        private class FixAllDiagnosticProvider : FixAllContext.DiagnosticProvider
        {
            private readonly TestDiagnosticAnalyzerDriver _testDriver;
            private readonly ImmutableHashSet<string> _diagnosticIds;

            public FixAllDiagnosticProvider(TestDiagnosticAnalyzerDriver testDriver, ImmutableHashSet<string> diagnosticIds)
            {
                _testDriver = testDriver;
                _diagnosticIds = diagnosticIds;
            }

            public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var diags = await _testDriver.GetDocumentDiagnosticsAsync(document, root.FullSpan);
                diags = diags.Where(diag => _diagnosticIds.Contains(diag.Id));
                return diags;
            }

            public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
                => GetProjectDiagnosticsAsync(project, true);

            public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
                => GetProjectDiagnosticsAsync(project, false);

            private async Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(
                Project project, bool includeAllDocumentDiagnostics)
            {
                var diags = includeAllDocumentDiagnostics
                    ? await _testDriver.GetAllDiagnosticsAsync(project)
                    : await _testDriver.GetProjectDiagnosticsAsync(project);
                diags = diags.Where(diag => _diagnosticIds.Contains(diag.Id));
                return diags;
            }
        }
    }
}
