// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// FixAll context with some additional information specifically for <see cref="FixAllCodeAction"/>.
    /// </summary>
    internal partial class FixAllCodeActionContext : FixAllContext
    {
        internal class FixAllDiagnosticProvider : DiagnosticProvider
        {
            private readonly ImmutableHashSet<string> _diagnosticIds;

            /// <summary>
            /// Delegate to fetch diagnostics for any given document within the given fix all scope.
            /// This delegate is invoked by <see cref="GetDocumentDiagnosticsAsync(Document, CancellationToken)"/> with the given <see cref="_diagnosticIds"/> as arguments.
            /// </summary>
            private readonly Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> _getDocumentDiagnosticsAsync;

            /// <summary>
            /// Delegate to fetch diagnostics for any given project within the given fix all scope.
            /// This delegate is invoked by <see cref="GetProjectDiagnosticsAsync(Project, CancellationToken)"/> and <see cref="GetAllDiagnosticsAsync(Project, CancellationToken)"/>
            /// with the given <see cref="_diagnosticIds"/> as arguments.
            /// The boolean argument to the delegate indicates whether or not to return location-based diagnostics, i.e.
            /// (a) False => Return only diagnostics with <see cref="Location.None"/>.
            /// (b) True => Return all project diagnostics, regardless of whether or not they have a location.
            /// </summary>
            private readonly Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> _getProjectDiagnosticsAsync;

            public FixAllDiagnosticProvider(
                ImmutableHashSet<string> diagnosticIds,
                Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
                Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync)
            {
                _diagnosticIds = diagnosticIds;
                _getDocumentDiagnosticsAsync = getDocumentDiagnosticsAsync;
                _getProjectDiagnosticsAsync = getProjectDiagnosticsAsync;
            }

            public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
            {
                return _getDocumentDiagnosticsAsync(document, _diagnosticIds, cancellationToken);
            }

            public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                return _getProjectDiagnosticsAsync(project, true, _diagnosticIds, cancellationToken);
            }

            public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                return _getProjectDiagnosticsAsync(project, false, _diagnosticIds, cancellationToken);
            }
        }
    }
}
