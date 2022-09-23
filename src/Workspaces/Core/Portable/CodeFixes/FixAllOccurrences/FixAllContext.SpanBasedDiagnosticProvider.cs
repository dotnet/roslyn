// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Context for "Fix all occurrences" code fixes provided by a <see cref="FixAllProvider"/>.
    /// </summary>
    public partial class FixAllContext
    {
        /// <summary>
        /// Diagnostic provider to fetch document/project diagnostics to fix in a <see cref="FixAllContext"/>,
        /// which supports a <see cref="GetDocumentSpanDiagnosticsAsync(Document, TextSpan, CancellationToken)"/>
        /// method to compute diagnostics for a given span within a document.
        /// We need to compute diagnostics for a span when applying a fix all operation in <see cref="FixAllScope.ContainingMember"/>
        /// and <see cref="FixAllScope.ContainingType"/> scopes.
        /// A regular <see cref="DiagnosticProvider"/> will compute diagnostics for the entire document and filter out
        /// diagnostics outside the span as a post-filtering step.
        /// A <see cref="SpanBasedDiagnosticProvider"/> can do this more efficiently by implementing the
        /// <see cref="GetDocumentSpanDiagnosticsAsync(Document, TextSpan, CancellationToken)"/> method to compute
        /// the diagnostics only for the given 'fixAllSpan' upfront.
        /// </summary>
        internal abstract class SpanBasedDiagnosticProvider : DiagnosticProvider
        {
            /// <summary>
            /// Gets all the diagnostics to fix for the given <paramref name="fixAllSpan"/> in the given <paramref name="document"/> in a <see cref="FixAllContext"/>.
            /// </summary>
            public abstract Task<IEnumerable<Diagnostic>> GetDocumentSpanDiagnosticsAsync(Document document, TextSpan fixAllSpan, CancellationToken cancellationToken);
        }
    }
}
