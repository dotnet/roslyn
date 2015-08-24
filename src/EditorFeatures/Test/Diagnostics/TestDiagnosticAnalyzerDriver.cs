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

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public class TestDiagnosticAnalyzerDriver : IDisposable
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> _workspaceAnalyzers;
        private readonly TestDiagnosticAnalyzerService _diagnosticAnalyzerService;
        private readonly TestHostDiagnosticUpdateSource _exceptionDiagnosticsSource;
        private readonly SolutionCrawler.IIncrementalAnalyzer _incrementalAnalyzer;
        private readonly Action<Exception, DiagnosticAnalyzer, Diagnostic> _onAnalyzerException;

        public TestDiagnosticAnalyzerDriver(Project project, DiagnosticAnalyzer workspaceAnalyzerOpt = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false)
            : this (project, ImmutableArray.Create(workspaceAnalyzerOpt ?? DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(project.Language)), onAnalyzerException, logAnalyzerExceptionAsDiagnostics)
        { }

        public TestDiagnosticAnalyzerDriver(Project project, ImmutableArray<DiagnosticAnalyzer> workspaceAnalyzers, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false)
        {
            _workspaceAnalyzers = workspaceAnalyzers;
            _exceptionDiagnosticsSource = new TestHostDiagnosticUpdateSource(project.Solution.Workspace);
            _diagnosticAnalyzerService = new TestDiagnosticAnalyzerService(project.Language, workspaceAnalyzers, _exceptionDiagnosticsSource, onAnalyzerException);
            _incrementalAnalyzer = _diagnosticAnalyzerService.CreateIncrementalAnalyzer(project.Solution.Workspace);

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
                var tree = document.GetSyntaxTreeAsync().Result;
                var root = tree.GetRoot();
                var dxs = _diagnosticAnalyzerService.GetDiagnosticsAsync(project.Solution, project.Id, document.Id).WaitAndGetResult(CancellationToken.None);
                documentDiagnostics = dxs.Where(d => d.HasTextSpan && d.TextSpan.IntersectsWith(span)).Select(d => d.ToDiagnostic(tree));
            }

            if (getProjectDiagnostics)
            {
                var dxs = _diagnosticAnalyzerService.GetDiagnosticsAsync(project.Solution, project.Id).WaitAndGetResult(CancellationToken.None);
                projectDiagnostics = dxs.Where(d => !d.HasTextSpan).Select(d => d.ToDiagnostic(tree: null));
            }

            var exceptionDiagnostics = _exceptionDiagnosticsSource.TestOnly_GetReportedDiagnostics().Select(d => d.ToDiagnostic(tree: null));

            return documentDiagnostics.Concat(projectDiagnostics).Concat(exceptionDiagnostics);
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
