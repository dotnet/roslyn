// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.Testing
{
    internal sealed class TestDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        private readonly ImmutableArray<(Project project, Diagnostic diagnostic)> _diagnostics;

        private TestDiagnosticProvider(ImmutableArray<(Project project, Diagnostic diagnostic)> diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<Diagnostic>>(_diagnostics.Where(diagnostic => diagnostic.project.Id == project.Id).Select(diagnostic => diagnostic.diagnostic));

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
            => Task.FromResult(_diagnostics.Where(i => i.diagnostic.Location.GetLineSpan().Path == document.FilePath).Where(diagnostic => diagnostic.project.Id == document.Project.Id).Select(diagnostic => diagnostic.diagnostic));

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            => Task.FromResult(_diagnostics.Where(i => !i.diagnostic.Location.IsInSource).Where(diagnostic => diagnostic.project.Id == project.Id).Select(diagnostic => diagnostic.diagnostic));

        internal static TestDiagnosticProvider Create(ImmutableArray<(Project project, Diagnostic diagnostic)> diagnostics) => new(diagnostics);
    }
}
