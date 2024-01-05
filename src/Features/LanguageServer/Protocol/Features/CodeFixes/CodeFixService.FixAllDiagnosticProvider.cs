// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal partial class CodeFixService
    {
        private sealed class FixAllDiagnosticProvider : FixAllContext.SpanBasedDiagnosticProvider
        {
            private readonly IDiagnosticAnalyzerService _diagnosticService;
            private readonly ImmutableHashSet<string>? _diagnosticIds;
            private readonly bool _includeSuppressedDiagnostics;

            public FixAllDiagnosticProvider(IDiagnosticAnalyzerService diagnosticService, ImmutableHashSet<string> diagnosticIds)
            {
                _diagnosticService = diagnosticService;

                // When computing FixAll for unnecessary pragma suppression diagnostic,
                // we need to include suppressed diagnostics, as well as reported compiler and analyzer diagnostics.
                // A null value for '_diagnosticIds' ensures the latter.
                if (diagnosticIds.Contains(IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId))
                {
                    _diagnosticIds = null;
                    _includeSuppressedDiagnostics = true;
                }
                else
                {
                    _diagnosticIds = diagnosticIds;
                    _includeSuppressedDiagnostics = false;
                }
            }

            public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
            {
                var solution = document.Project.Solution;
                var diagnostics = await _diagnosticService.GetDiagnosticsForIdsAsync(solution, projectId: null, document.Id, _diagnosticIds, shouldIncludeAnalyzer: null, _includeSuppressedDiagnostics, includeLocalDocumentDiagnostics: true, includeNonLocalDocumentDiagnostics: false, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfFalse(diagnostics.All(d => d.DocumentId != null));
                return await diagnostics.ToDiagnosticsAsync(document.Project, cancellationToken).ConfigureAwait(false);
            }

            public override async Task<IEnumerable<Diagnostic>> GetDocumentSpanDiagnosticsAsync(Document document, TextSpan fixAllSpan, CancellationToken cancellationToken)
            {
                bool shouldIncludeDiagnostic(string id) => _diagnosticIds == null || _diagnosticIds.Contains(id);
                var diagnostics = await _diagnosticService.GetDiagnosticsForSpanAsync(document, fixAllSpan, shouldIncludeDiagnostic,
                    includeCompilerDiagnostics: true, _includeSuppressedDiagnostics, priorityProvider: new DefaultCodeActionRequestPriorityProvider(),
                    addOperationScope: null, DiagnosticKind.All, isExplicit: false, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfFalse(diagnostics.All(d => d.DocumentId != null));
                return await diagnostics.ToDiagnosticsAsync(document.Project, cancellationToken).ConfigureAwait(false);
            }

            public override async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                // Get all diagnostics for the entire project, including document diagnostics.
                var diagnostics = await _diagnosticService.GetDiagnosticsForIdsAsync(project.Solution, project.Id, documentId: null, _diagnosticIds, shouldIncludeAnalyzer: null, _includeSuppressedDiagnostics, includeLocalDocumentDiagnostics: true, includeNonLocalDocumentDiagnostics: false, cancellationToken).ConfigureAwait(false);
                return await diagnostics.ToDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
            }

            public override async Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                // Get all no-location diagnostics for the project, doesn't include document diagnostics.
                var diagnostics = await _diagnosticService.GetProjectDiagnosticsForIdsAsync(project.Solution, project.Id, _diagnosticIds, shouldIncludeAnalyzer: null, _includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics: false, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfFalse(diagnostics.All(d => d.DocumentId == null));
                return await diagnostics.ToDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
