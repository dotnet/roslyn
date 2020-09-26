// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal partial class CodeFixService
    {
        private class FixAllDiagnosticProvider : FixAllContext.DiagnosticProvider
        {
            private readonly CodeFixService _codeFixService;
            private readonly ImmutableHashSet<string>? _diagnosticIds;
            private readonly bool _includeSuppressedDiagnostics;

            public FixAllDiagnosticProvider(CodeFixService codeFixService, ImmutableHashSet<string>? diagnosticIds, bool includeSuppressedDiagnostics)
            {
                Debug.Assert(diagnosticIds == null || !diagnosticIds.Contains(IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId));
                Debug.Assert(!includeSuppressedDiagnostics || diagnosticIds == null);

                _codeFixService = codeFixService;
                _diagnosticIds = diagnosticIds;
                _includeSuppressedDiagnostics = includeSuppressedDiagnostics;
            }

            public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
                => _codeFixService.GetDocumentDiagnosticsAsync(document, _diagnosticIds, _includeSuppressedDiagnostics, cancellationToken);

            public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
                => _codeFixService.GetProjectDiagnosticsAsync(project, true, _diagnosticIds, _includeSuppressedDiagnostics, cancellationToken);

            public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
                => _codeFixService.GetProjectDiagnosticsAsync(project, false, _diagnosticIds, _includeSuppressedDiagnostics, cancellationToken);
        }
    }
}
