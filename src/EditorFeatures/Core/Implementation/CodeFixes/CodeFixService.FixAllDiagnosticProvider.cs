// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal partial class CodeFixService
    {
        private class FixAllDiagnosticProvider : FixAllContext.DiagnosticProvider
        {
            private readonly CodeFixService _codeFixService;
            private readonly ImmutableHashSet<string> _diagnosticIds;

            public FixAllDiagnosticProvider(CodeFixService codeFixService, ImmutableHashSet<string> diagnosticIds)
            {
                _codeFixService = codeFixService;
                _diagnosticIds = diagnosticIds;
            }

            public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
                => _codeFixService.GetDocumentDiagnosticsAsync(document, _diagnosticIds, cancellationToken);

            public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
                => _codeFixService.GetProjectDiagnosticsAsync(project, true, _diagnosticIds, cancellationToken);

            public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
                => _codeFixService.GetProjectDiagnosticsAsync(project, false, _diagnosticIds, cancellationToken);
        }

        private class FixAllPredefinedDiagnosticProvider : FixAllContext.DiagnosticProvider
        {
            private readonly ImmutableArray<Diagnostic> _diagnostics;

            public FixAllPredefinedDiagnosticProvider(ImmutableArray<Diagnostic> diagnostics)
            {
                _diagnostics = diagnostics;
            }

            public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
                => Task.FromResult<IEnumerable<Diagnostic>>(_diagnostics);

            public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
                => Task.FromResult<IEnumerable<Diagnostic>>(_diagnostics);

            public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
                => SpecializedTasks.EmptyEnumerable<Diagnostic>();
        }
    }
}
