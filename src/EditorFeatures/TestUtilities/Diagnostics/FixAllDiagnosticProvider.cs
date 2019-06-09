// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public abstract partial class AbstractUserDiagnosticTest
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
                => GetProjectDiagnosticsAsync(project, true, cancellationToken);

            public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
                => GetProjectDiagnosticsAsync(project, false, cancellationToken);

            private async Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(
                Project project, bool includeAllDocumentDiagnostics, CancellationToken cancellationToken)
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
