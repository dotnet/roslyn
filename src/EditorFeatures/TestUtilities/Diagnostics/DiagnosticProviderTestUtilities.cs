// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public static class DiagnosticProviderTestUtilities
    {
        public static async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(
            Document document,
            TextSpan span,
            bool includeSuppressedDiagnostics = false)
        {
            var testDriver = new TestDiagnosticAnalyzerDriver(document.Project, includeSuppressedDiagnostics);
            return await testDriver.GetAllDiagnosticsAsync(document, span);
        }

        public static async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(
            Document document,
            TextSpan span,
            bool includeSuppressedDiagnostics = false)
        {
            var testDriver = new TestDiagnosticAnalyzerDriver(document.Project, includeSuppressedDiagnostics);
            return await testDriver.GetDocumentDiagnosticsAsync(document, span);
        }

        public static async Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(
            Project project,
            bool includeSuppressedDiagnostics = false)
        {
            var testDriver = new TestDiagnosticAnalyzerDriver(project, includeSuppressedDiagnostics);
            return await testDriver.GetProjectDiagnosticsAsync(project);
        }
    }
}
