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
        private readonly ImmutableArray<Diagnostic> _diagnostics;

        private TestDiagnosticProvider(ImmutableArray<Diagnostic> diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<Diagnostic>>(_diagnostics);

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
            => Task.FromResult(_diagnostics.Where(i => i.Location.GetLineSpan().Path == document.Name));

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            => Task.FromResult(_diagnostics.Where(i => !i.Location.IsInSource));

        internal static TestDiagnosticProvider Create(ImmutableArray<Diagnostic> diagnostics) => new TestDiagnosticProvider(diagnostics);
    }
}
