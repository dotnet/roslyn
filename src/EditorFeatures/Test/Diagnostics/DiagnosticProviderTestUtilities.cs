// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public static class DiagnosticProviderTestUtilities
    {
        private static IEnumerable<Diagnostic> GetDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span, Project project, bool getDocumentDiagnostics, bool getProjectDiagnostics, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException, bool logAnalyzerExceptionAsDiagnostics)
        {
            var documentDiagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();
            var projectDiagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();

            // If no user diagnostic analyzer, then test compiler diagnostics.
            var workspaceAnalyzer = workspaceAnalyzerOpt ?? DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(project.Language);

            // If the test is not configured with a custom onAnalyzerException handler AND has not requested exceptions to be handled and logged as diagnostics, then FailFast on exceptions.
            if (onAnalyzerException == null && !logAnalyzerExceptionAsDiagnostics)
            {
                onAnalyzerException = DiagnosticExtensions.FailFastOnAnalyzerException;
            }

            var exceptionDiagnosticsSource = new TestHostDiagnosticUpdateSource(project.Solution.Workspace);
            var analyzerService = new TestDiagnosticAnalyzerService(project.Language, workspaceAnalyzer, exceptionDiagnosticsSource, onAnalyzerException);
            var incrementalAnalyzer = analyzerService.CreateIncrementalAnalyzer(project.Solution.Workspace);

            if (getDocumentDiagnostics)
            {
                var tree = document.GetSyntaxTreeAsync().Result;
                var root = tree.GetRoot();
                var dxs = analyzerService.GetDiagnosticsAsync(project.Solution, project.Id, document.Id).WaitAndGetResult(CancellationToken.None);
                documentDiagnostics = dxs.Where(d => d.HasTextSpan && d.TextSpan.IntersectsWith(span)).Select(d => d.ToDiagnostic(tree));
            }

            if (getProjectDiagnostics)
            {
                var dxs = analyzerService.GetDiagnosticsAsync(project.Solution, project.Id).WaitAndGetResult(CancellationToken.None);
                projectDiagnostics = dxs.Where(d => !d.HasTextSpan).Select(d => d.ToDiagnostic(tree: null));
            }

            var exceptionDiagnostics = exceptionDiagnosticsSource.TestOnly_GetReportedDiagnostics(workspaceAnalyzer).Select(d => d.ToDiagnostic(tree: null));

            return documentDiagnostics.Concat(projectDiagnostics).Concat(exceptionDiagnostics);
        }

        public static IEnumerable<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false)
        {
            return GetDiagnostics(workspaceAnalyzerOpt, document, span, document.Project, getDocumentDiagnostics: true, getProjectDiagnostics: true, onAnalyzerException: onAnalyzerException, logAnalyzerExceptionAsDiagnostics: logAnalyzerExceptionAsDiagnostics);
        }

        public static IEnumerable<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Project project, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var document in project.Documents)
            {
                var span = document.GetSyntaxRootAsync().Result.FullSpan;
                var documentDiagnostics = GetDocumentDiagnostics(workspaceAnalyzerOpt, document, span, onAnalyzerException, logAnalyzerExceptionAsDiagnostics);
                diagnostics.AddRange(documentDiagnostics);
            }

            var projectDiagnostics = GetProjectDiagnostics(workspaceAnalyzerOpt, project, onAnalyzerException, logAnalyzerExceptionAsDiagnostics);
            diagnostics.AddRange(projectDiagnostics);
            return diagnostics;
        }

        public static IEnumerable<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Solution solution, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var project in solution.Projects)
            {
                var projectDiagnostics = GetAllDiagnostics(workspaceAnalyzerOpt, project, onAnalyzerException, logAnalyzerExceptionAsDiagnostics);
                diagnostics.AddRange(projectDiagnostics);
            }

            return diagnostics;
        }

        public static IEnumerable<Diagnostic> GetDocumentDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false)
        {
            return GetDiagnostics(workspaceAnalyzerOpt, document, span, document.Project, getDocumentDiagnostics: true, getProjectDiagnostics: false, onAnalyzerException: onAnalyzerException, logAnalyzerExceptionAsDiagnostics: logAnalyzerExceptionAsDiagnostics);
        }

        public static IEnumerable<Diagnostic> GetProjectDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Project project, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false)
        {
            return GetDiagnostics(workspaceAnalyzerOpt, null, default(TextSpan), project, getDocumentDiagnostics: false, getProjectDiagnostics: true, onAnalyzerException: onAnalyzerException, logAnalyzerExceptionAsDiagnostics: logAnalyzerExceptionAsDiagnostics);
        }
    }
}
