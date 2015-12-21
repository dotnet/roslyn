// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;
using Xunit;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public class TestDiagnosticAnalyzerDriver : IDisposable
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> _workspaceAnalyzers;
        private readonly TestDiagnosticAnalyzerService _diagnosticAnalyzerService;
        private readonly TestHostDiagnosticUpdateSource _exceptionDiagnosticsSource;
        private readonly SolutionCrawler.IIncrementalAnalyzer _incrementalAnalyzer;
        private readonly Action<Exception, DiagnosticAnalyzer, Diagnostic> _onAnalyzerException;
        private readonly bool _includeSuppressedDiagnostics;

        public TestDiagnosticAnalyzerDriver(Project project, DiagnosticAnalyzer workspaceAnalyzerOpt = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, bool includeSuppressedDiagnostics = false)
            : this(project, ImmutableArray.Create(workspaceAnalyzerOpt ?? DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(project.Language)), onAnalyzerException, logAnalyzerExceptionAsDiagnostics, includeSuppressedDiagnostics)
        { }

        public TestDiagnosticAnalyzerDriver(Project project, ImmutableArray<DiagnosticAnalyzer> workspaceAnalyzers, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, bool includeSuppressedDiagnostics = false)
        {
            _workspaceAnalyzers = workspaceAnalyzers;
            _exceptionDiagnosticsSource = new TestHostDiagnosticUpdateSource(project.Solution.Workspace);
            _diagnosticAnalyzerService = new TestDiagnosticAnalyzerService(project.Language, workspaceAnalyzers, _exceptionDiagnosticsSource, onAnalyzerException);
            _incrementalAnalyzer = _diagnosticAnalyzerService.CreateIncrementalAnalyzer(project.Solution.Workspace);
            _includeSuppressedDiagnostics = includeSuppressedDiagnostics;

            // If the test is not configured with a custom onAnalyzerException handler AND has not requested exceptions to be handled and logged as diagnostics, then FailFast on exceptions.
            if (onAnalyzerException == null && !logAnalyzerExceptionAsDiagnostics)
            {
                onAnalyzerException = DiagnosticExtensions.FailFastOnAnalyzerException;
            }

            _onAnalyzerException = onAnalyzerException;
        }

        private async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span, Project project, bool getDocumentDiagnostics, bool getProjectDiagnostics)
        {
            var documentDiagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();
            var projectDiagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();

            if (getDocumentDiagnostics)
            {
                var dxs = await _diagnosticAnalyzerService.GetDiagnosticsAsync(project.Solution, project.Id, document.Id, _includeSuppressedDiagnostics);
                documentDiagnostics = await CodeAnalysis.Diagnostics.Extensions.ToDiagnosticsAsync(dxs.Where(d => d.HasTextSpan && d.TextSpan.IntersectsWith(span)), project, CancellationToken.None);
            }

            if (getProjectDiagnostics)
            {
                var dxs = await _diagnosticAnalyzerService.GetDiagnosticsAsync(project.Solution, project.Id, includeSuppressedDiagnostics: _includeSuppressedDiagnostics);
                projectDiagnostics = await CodeAnalysis.Diagnostics.Extensions.ToDiagnosticsAsync(dxs.Where(d => !d.HasTextSpan), project, CancellationToken.None);
            }

            var exceptionDiagnostics = await CodeAnalysis.Diagnostics.Extensions.ToDiagnosticsAsync(_exceptionDiagnosticsSource.TestOnly_GetReportedDiagnostics(), project, CancellationToken.None);
            var allDiagnostics = documentDiagnostics.Concat(projectDiagnostics).Concat(exceptionDiagnostics);

            if (!_includeSuppressedDiagnostics)
            {
                Assert.True(!allDiagnostics.Any(d => d.IsSuppressed));
            }

            return allDiagnostics;
        }

        public Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span)
        {
            return GetDiagnosticsAsync(workspaceAnalyzerOpt, document, span, document.Project, getDocumentDiagnostics: true, getProjectDiagnostics: true);
        }

        public async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(DiagnosticAnalyzer workspaceAnalyzerOpt, Project project)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var document in project.Documents)
            {
                var span = document.GetSyntaxRootAsync().Result.FullSpan;
                var documentDiagnostics = await GetDocumentDiagnosticsAsync(workspaceAnalyzerOpt, document, span);
                diagnostics.AddRange(documentDiagnostics);
            }

            var projectDiagnostics = await GetProjectDiagnosticsAsync(workspaceAnalyzerOpt, project);
            diagnostics.AddRange(projectDiagnostics);
            return diagnostics;
        }

        public async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(DiagnosticAnalyzer workspaceAnalyzerOpt, Solution solution)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var project in solution.Projects)
            {
                var projectDiagnostics = await GetAllDiagnosticsAsync(workspaceAnalyzerOpt, project);
                diagnostics.AddRange(projectDiagnostics);
            }

            return diagnostics;
        }

        public Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span)
        {
            return GetDiagnosticsAsync(workspaceAnalyzerOpt, document, span, document.Project, getDocumentDiagnostics: true, getProjectDiagnostics: false);
        }

        public Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(DiagnosticAnalyzer workspaceAnalyzerOpt, Project project)
        {
            return GetDiagnosticsAsync(workspaceAnalyzerOpt, null, default(TextSpan), project, getDocumentDiagnostics: false, getProjectDiagnostics: true);
        }

        public void Dispose()
        {
            CompilationWithAnalyzers.ClearAnalyzerState(_workspaceAnalyzers);
        }
    }
}
