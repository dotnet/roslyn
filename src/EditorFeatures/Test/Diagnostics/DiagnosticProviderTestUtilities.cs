// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public static class DiagnosticProviderTestUtilities
    {
        public static async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, bool includeSuppressedDiagnostics = false)
        {
            using (var testDriver = new TestDiagnosticAnalyzerDriver(document.Project, workspaceAnalyzerOpt, onAnalyzerException, logAnalyzerExceptionAsDiagnostics, includeSuppressedDiagnostics))
            {
                return await testDriver.GetAllDiagnosticsAsync(workspaceAnalyzerOpt, document, span);
            }
        }

        public static async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(DiagnosticAnalyzer workspaceAnalyzerOpt, Project project, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, bool includeSuppressedDiagnostics = false)
        {
            using (var testDriver = new TestDiagnosticAnalyzerDriver(project, workspaceAnalyzerOpt, onAnalyzerException, logAnalyzerExceptionAsDiagnostics, includeSuppressedDiagnostics))
            {
                return await testDriver.GetAllDiagnosticsAsync(workspaceAnalyzerOpt, project);
            }
        }

        public static async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(DiagnosticAnalyzer workspaceAnalyzerOpt, Solution solution, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, bool includeSuppressedDiagnostics = false)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var project in solution.Projects)
            {
                var projectDiagnostics = await GetAllDiagnosticsAsync(workspaceAnalyzerOpt, project, onAnalyzerException, logAnalyzerExceptionAsDiagnostics, includeSuppressedDiagnostics);
                diagnostics.AddRange(projectDiagnostics);
            }

            return diagnostics;
        }

        public static async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(DiagnosticAnalyzer workspaceAnalyzerOpt, Document document, TextSpan span, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, bool includeSuppressedDiagnostics = false)
        {
            using (var testDriver = new TestDiagnosticAnalyzerDriver(document.Project, workspaceAnalyzerOpt, onAnalyzerException, logAnalyzerExceptionAsDiagnostics, includeSuppressedDiagnostics))
            {
                return await testDriver.GetDocumentDiagnosticsAsync(workspaceAnalyzerOpt, document, span);
            }
        }

        public static async Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(DiagnosticAnalyzer workspaceAnalyzerOpt, Project project, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, bool includeSuppressedDiagnostics = false)
        {
            using (var testDriver = new TestDiagnosticAnalyzerDriver(project, workspaceAnalyzerOpt, onAnalyzerException, logAnalyzerExceptionAsDiagnostics, includeSuppressedDiagnostics))
            {
                return await testDriver.GetProjectDiagnosticsAsync(workspaceAnalyzerOpt, project);
            }
        }
    }
}
