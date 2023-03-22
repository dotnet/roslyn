// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public static class DiagnosticProviderTestUtilities
    {
        public static async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(
            Workspace workspace,
            Document document,
            TextSpan span,
            bool includeSuppressedDiagnostics = false,
            bool includeNonLocalDocumentDiagnostics = false)
        {
            var testDriver = new TestDiagnosticAnalyzerDriver(workspace, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics);
            return await testDriver.GetAllDiagnosticsAsync(document, span);
        }

        public static async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(
            Workspace workspace,
            Document document,
            TextSpan span,
            bool includeSuppressedDiagnostics = false,
            bool includeNonLocalDocumentDiagnostics = false)
        {
            var testDriver = new TestDiagnosticAnalyzerDriver(workspace, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics);
            return await testDriver.GetDocumentDiagnosticsAsync(document, span);
        }

        public static async Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(
            Workspace workspace,
            Project project,
            bool includeSuppressedDiagnostics = false,
            bool includeNonLocalDocumentDiagnostics = false)
        {
            var testDriver = new TestDiagnosticAnalyzerDriver(workspace, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics);
            return await testDriver.GetProjectDiagnosticsAsync(project);
        }
    }
}
