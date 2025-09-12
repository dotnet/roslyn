// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes;

internal sealed partial class CodeFixService
{
    private sealed class FixAllDiagnosticProvider : FixAllContext.SpanBasedDiagnosticProvider
    {
        private readonly ImmutableHashSet<string>? _diagnosticIds;
        private readonly bool _includeSuppressedDiagnostics;

        public FixAllDiagnosticProvider(ImmutableHashSet<string> diagnosticIds)
        {
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

        private ImmutableArray<DiagnosticData> Filter(ImmutableArray<DiagnosticData> diagnostics)
            => diagnostics.WhereAsArray(d => _includeSuppressedDiagnostics || !d.IsSuppressed);

        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var service = document.Project.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
            var diagnostics = Filter(await service.GetDiagnosticsForIdsAsync(
                document.Project, [document.Id], _diagnosticIds, AnalyzerFilter.All, includeLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false));
            Contract.ThrowIfFalse(diagnostics.All(d => d.DocumentId != null));
            return await diagnostics.ToDiagnosticsAsync(document.Project, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<Diagnostic>> GetDocumentSpanDiagnosticsAsync(Document document, TextSpan fixAllSpan, CancellationToken cancellationToken)
        {
            var service = document.Project.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
            var diagnostics = Filter(await service.GetDiagnosticsForSpanAsync(
                document, fixAllSpan, DiagnosticIdFilter.Include(_diagnosticIds),
                priority: null, DiagnosticKind.All, cancellationToken).ConfigureAwait(false));
            Contract.ThrowIfFalse(diagnostics.All(d => d.DocumentId != null));
            return await diagnostics.ToDiagnosticsAsync(document.Project, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            // Get all diagnostics for the entire project, including document diagnostics.
            var service = project.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
            var diagnostics = Filter(await service.GetDiagnosticsForIdsAsync(
                project, documentIds: default, _diagnosticIds, AnalyzerFilter.All, includeLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false));
            return await diagnostics.ToDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            // Get all no-location diagnostics for the project, doesn't include document diagnostics.
            var service = project.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
            var diagnostics = Filter(await service.GetProjectDiagnosticsForIdsAsync(
                project, _diagnosticIds, AnalyzerFilter.All, cancellationToken).ConfigureAwait(false));
            Contract.ThrowIfFalse(diagnostics.All(d => d.DocumentId == null));
            return await diagnostics.ToDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
        }
    }
}
