// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts
{
    /// <summary>
    /// Source for hot reload diagnostics.
    /// </summary>
    internal interface IHotReloadDiagnosticSource
    {
        /// <summary>
        /// Provides list of document ids that have hot reload diagnostics.
        /// </summary>
        ValueTask<ImmutableArray<DocumentId>> GetDocumentIdsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Provides list of diagnostics for the given document.
        /// </summary>
        ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(TextDocument document, CancellationToken cancellationToken);
    }
}
