// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace AnalyzerRunner
{
    internal sealed class TesterDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        private readonly ImmutableDictionary<ProjectId, ImmutableDictionary<string, ImmutableArray<Diagnostic>>> _documentDiagnostics;
        private readonly ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>> _projectDiagnostics;

        public TesterDiagnosticProvider(ImmutableDictionary<ProjectId, ImmutableDictionary<string, ImmutableArray<Diagnostic>>> documentDiagnostics, ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>> projectDiagnostics)
        {
            _documentDiagnostics = documentDiagnostics;
            _projectDiagnostics = projectDiagnostics;
        }

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            if (!_projectDiagnostics.TryGetValue(project.Id, out var filteredProjectDiagnostics))
            {
                filteredProjectDiagnostics = ImmutableArray<Diagnostic>.Empty;
            }

            if (!_documentDiagnostics.TryGetValue(project.Id, out var filteredDocumentDiagnostics))
            {
                filteredDocumentDiagnostics = ImmutableDictionary<string, ImmutableArray<Diagnostic>>.Empty;
            }

            return Task.FromResult(filteredProjectDiagnostics.Concat(filteredDocumentDiagnostics.Values.SelectMany(i => i)));
        }

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            if (!_documentDiagnostics.TryGetValue(document.Project.Id, out var projectDocumentDiagnostics))
            {
                return Task.FromResult(Enumerable.Empty<Diagnostic>());
            }

            if (!projectDocumentDiagnostics.TryGetValue(document.FilePath, out var diagnostics))
            {
                return Task.FromResult(Enumerable.Empty<Diagnostic>());
            }

            return Task.FromResult(diagnostics.AsEnumerable());
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            if (!_projectDiagnostics.TryGetValue(project.Id, out var diagnostics))
            {
                return Task.FromResult(Enumerable.Empty<Diagnostic>());
            }

            return Task.FromResult(diagnostics.AsEnumerable());
        }
    }
}
