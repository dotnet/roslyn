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

        private IEnumerable<Diagnostic> GetDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span, Project project, bool getDocumentDiagnostics, bool getProjectDiagnostics)
        {
            var documentDiagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();
            var projectDiagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();

            if (getDocumentDiagnostics)
            {
                var dxs = _diagnosticAnalyzerService.GetDiagnosticsAsync(project.Solution, project.Id, document.Id, _includeSuppressedDiagnostics).WaitAndGetResult(CancellationToken.None);
                documentDiagnostics = Microsoft.CodeAnalysis.Diagnostics.Extensions.ToDiagnosticsAsync(dxs.Where(d => d.HasTextSpan && d.TextSpan.IntersectsWith(span)), project, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            }

            if (getProjectDiagnostics)
            {
                var dxs = _diagnosticAnalyzerService.GetDiagnosticsAsync(project.Solution, project.Id, includeSuppressedDiagnostics: _includeSuppressedDiagnostics).WaitAndGetResult(CancellationToken.None);
                projectDiagnostics = Microsoft.CodeAnalysis.Diagnostics.Extensions.ToDiagnosticsAsync(dxs.Where(d => !d.HasTextSpan), project, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            }

            var exceptionDiagnostics = Microsoft.CodeAnalysis.Diagnostics.Extensions.ToDiagnosticsAsync(_exceptionDiagnosticsSource.TestOnly_GetReportedDiagnostics(), project, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var allDiagnostics = documentDiagnostics.Concat(projectDiagnostics).Concat(exceptionDiagnostics);

            if (!_includeSuppressedDiagnostics)
            {
                Assert.True(!allDiagnostics.Any(d => d.IsSuppressed));
            }

            return allDiagnostics;
        }

        public IEnumerable<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span)
        {
            return GetDiagnostics(workspaceAnalyzerOpt, document, span, document.Project, getDocumentDiagnostics: true, getProjectDiagnostics: true);
        }

        public IEnumerable<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Project project)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var document in project.Documents)
            {
                var span = document.GetSyntaxRootAsync().Result.FullSpan;
                var documentDiagnostics = GetDocumentDiagnostics(workspaceAnalyzerOpt, document, span);
                diagnostics.AddRange(documentDiagnostics);
            }

            var projectDiagnostics = GetProjectDiagnostics(workspaceAnalyzerOpt, project);
            diagnostics.AddRange(projectDiagnostics);
            return diagnostics;
        }

        public IEnumerable<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Solution solution)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var project in solution.Projects)
            {
                var projectDiagnostics = GetAllDiagnostics(workspaceAnalyzerOpt, project);
                diagnostics.AddRange(projectDiagnostics);
            }

            return diagnostics;
        }

        public IEnumerable<Diagnostic> GetDocumentDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span)
        {
            return GetDiagnostics(workspaceAnalyzerOpt, document, span, document.Project, getDocumentDiagnostics: true, getProjectDiagnostics: false);
        }

        public IEnumerable<Diagnostic> GetProjectDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Project project)
        {
            return GetDiagnostics(workspaceAnalyzerOpt, null, default(TextSpan), project, getDocumentDiagnostics: false, getProjectDiagnostics: true);
        }

        public void Dispose()
        {
            CompilationWithAnalyzers.ClearAnalyzerState(_workspaceAnalyzers);
        }
    }
}
