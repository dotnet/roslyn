// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public static class DiagnosticProviderTestUtilities
    {
        public static IEnumerable<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, bool includeSuppressedDiagnostics = false)
        {
            using (var testDriver = new TestDiagnosticAnalyzerDriver(document.Project, workspaceAnalyzerOpt, onAnalyzerException, logAnalyzerExceptionAsDiagnostics, includeSuppressedDiagnostics))
            {
                return testDriver.GetAllDiagnostics(workspaceAnalyzerOpt, document, span);
            }
        }

        public static IEnumerable<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Project project, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, bool includeSuppressedDiagnostics = false)
        {
            using (var testDriver = new TestDiagnosticAnalyzerDriver(project, workspaceAnalyzerOpt, onAnalyzerException, logAnalyzerExceptionAsDiagnostics, includeSuppressedDiagnostics))
            {
                return testDriver.GetAllDiagnostics(workspaceAnalyzerOpt, project);
            }
        }

        public static IEnumerable<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Solution solution, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, bool includeSuppressedDiagnostics = false)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var project in solution.Projects)
            {
                var projectDiagnostics = GetAllDiagnostics(workspaceAnalyzerOpt, project, onAnalyzerException, logAnalyzerExceptionAsDiagnostics, includeSuppressedDiagnostics);
                diagnostics.AddRange(projectDiagnostics);
            }

            return diagnostics;
        }

        public static IEnumerable<Diagnostic> GetDocumentDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, bool includeSuppressedDiagnostics = false)
        {
            using (var testDriver = new TestDiagnosticAnalyzerDriver(document.Project, workspaceAnalyzerOpt, onAnalyzerException, logAnalyzerExceptionAsDiagnostics, includeSuppressedDiagnostics))
            {
                return testDriver.GetDocumentDiagnostics(workspaceAnalyzerOpt, document, span);
            }
        }

        public static IEnumerable<Diagnostic> GetProjectDiagnostics(DiagnosticAnalyzer workspaceAnalyzerOpt, Project project, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, bool includeSuppressedDiagnostics = false)
        {
            using (var testDriver = new TestDiagnosticAnalyzerDriver(project, workspaceAnalyzerOpt, onAnalyzerException, logAnalyzerExceptionAsDiagnostics, includeSuppressedDiagnostics))
            {
                return testDriver.GetProjectDiagnostics(workspaceAnalyzerOpt, project);
            }
        }
    }
}
