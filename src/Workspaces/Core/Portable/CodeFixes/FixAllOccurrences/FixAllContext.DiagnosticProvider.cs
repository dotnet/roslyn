// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Context for "Fix all occurrences" code fixes provided by an <see cref="FixAllProvider"/>.
    /// </summary>
    public partial class FixAllContext
    {
        /// <summary>
        /// Diagnostic provider to fetch document/project diagnostics to fix in a <see cref="FixAllContext"/>.
        /// </summary>
        public abstract class DiagnosticProvider
        {
            internal virtual bool IsFixMultiple => false;

            /// <summary>
            /// Gets all the diagnostics to fix in the given document in a <see cref="FixAllContext"/>.
            /// </summary>
            public abstract Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken);

            /// <summary>
            /// Gets all the project-level diagnostics to fix, i.e. diagnostics with no source location, in the given project in a <see cref="FixAllContext"/>.
            /// </summary>
            public abstract Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken);

            /// <summary>
            /// Gets all the diagnostics to fix in the given project in a <see cref="FixAllContext"/>.
            /// This includes both document-level diagnostics for all documents in the given project and project-level diagnostics, i.e. diagnostics with no source location, in the given project. 
            /// </summary>
            public abstract Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken);

            internal virtual Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
                BatchFixAllProvider batchFixer, FixAllContext context)
            {
                return batchFixer.GetDocumentDiagnosticsToFixAsync(context);
            }

            internal virtual Task<ImmutableDictionary<Project, ImmutableArray<Diagnostic>>> GetProjectDiagnosticsToFixAsync(
                BatchFixAllProvider batchFixer, FixAllContext context)
            {
                return batchFixer.GetProjectDiagnosticsToFixAsync(context);
            }
        }

        internal Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
            BatchFixAllProvider batchFixer)
        {
            return _diagnosticProvider.GetDocumentDiagnosticsToFixAsync(batchFixer, this);
        }

        internal Task<ImmutableDictionary<Project, ImmutableArray<Diagnostic>>> GetProjectDiagnosticsToFixAsync(
            BatchFixAllProvider batchFixer)
        {
            return _diagnosticProvider.GetProjectDiagnosticsToFixAsync(batchFixer, this);
        }
    }
}
