// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public class TestDiagnosticAnalyzerDriver
    {
        private readonly DiagnosticAnalyzerService _diagnosticAnalyzerService;
        private readonly bool _includeSuppressedDiagnostics;

        public TestDiagnosticAnalyzerDriver(Workspace workspace, Project project, bool includeSuppressedDiagnostics = false)
        {
            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(((IMefHostExportProvider)workspace.Services.HostServices).GetExportedValue<IDiagnosticUpdateSourceRegistrationService>());
            _diagnosticAnalyzerService = Assert.IsType<DiagnosticAnalyzerService>(((IMefHostExportProvider)workspace.Services.HostServices).GetExportedValue<IDiagnosticAnalyzerService>());
            _diagnosticAnalyzerService.CreateIncrementalAnalyzer(project.Solution.Workspace);
            _includeSuppressedDiagnostics = includeSuppressedDiagnostics;
        }

        private async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(
            Project project,
            Document document,
            TextSpan? filterSpan,
            bool getDocumentDiagnostics,
            bool getProjectDiagnostics)
        {
            var documentDiagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();
            var projectDiagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();

            if (getDocumentDiagnostics)
            {
                var dxs = await _diagnosticAnalyzerService.GetDiagnosticsAsync(project.Solution, project.Id, document.Id, _includeSuppressedDiagnostics);
                documentDiagnostics = await CodeAnalysis.Diagnostics.Extensions.ToDiagnosticsAsync(
                    filterSpan is null
                        ? dxs.Where(d => d.HasTextSpan)
                        : dxs.Where(d => d.HasTextSpan && d.GetTextSpan().IntersectsWith(filterSpan.Value)),
                    project,
                    CancellationToken.None);
            }

            if (getProjectDiagnostics)
            {
                var dxs = await _diagnosticAnalyzerService.GetDiagnosticsAsync(project.Solution, project.Id, includeSuppressedDiagnostics: _includeSuppressedDiagnostics);
                projectDiagnostics = await CodeAnalysis.Diagnostics.Extensions.ToDiagnosticsAsync(dxs.Where(d => !d.HasTextSpan), project, CancellationToken.None);
            }

            var allDiagnostics = documentDiagnostics.Concat(projectDiagnostics);

            if (!_includeSuppressedDiagnostics)
            {
                Assert.True(!allDiagnostics.Any(d => d.IsSuppressed));
            }

            return allDiagnostics;
        }

        public Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Document document, TextSpan? filterSpan)
            => GetDiagnosticsAsync(document.Project, document, filterSpan, getDocumentDiagnostics: true, getProjectDiagnostics: true);

        public async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var document in project.Documents)
            {
                var span = (await document.GetSyntaxRootAsync()).FullSpan;
                var documentDiagnostics = await GetDocumentDiagnosticsAsync(document, span);
                diagnostics.AddRange(documentDiagnostics);
            }

            var projectDiagnostics = await GetProjectDiagnosticsAsync(project);
            diagnostics.AddRange(projectDiagnostics);
            return diagnostics;
        }

        public Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, TextSpan span)
            => GetDiagnosticsAsync(document.Project, document, span, getDocumentDiagnostics: true, getProjectDiagnostics: false);

        public Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project)
            => GetDiagnosticsAsync(project, document: null, filterSpan: null, getDocumentDiagnostics: false, getProjectDiagnostics: true);
    }
}
