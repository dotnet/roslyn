// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Lightweight analog to <see cref="DocumentSpan"/> that should be used in features that care about
    /// pointing at a particular location in a <see cref="Document"/> but do not want to root a potentially
    /// very stale <see cref="Solution"/> snapshot that may keep around a lot of memory in a host.
    /// </summary>
    internal readonly struct DocumentIdSpan
    {
        private readonly Workspace _workspace;
        private readonly DocumentId _documentId;
        public readonly TextSpan SourceSpan;

        public DocumentIdSpan(DocumentSpan documentSpan)
        {
            _workspace = documentSpan.Document.Project.Solution.Workspace;
            _documentId = documentSpan.Document.Id;
            SourceSpan = documentSpan.SourceSpan;
        }

        public async Task<DocumentSpan?> TryRehydrateAsync(CancellationToken cancellationToken)
        {
            var solution = _workspace.CurrentSolution;
            var document = await solution.GetDocumentAsync(_documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            return document == null ? null : new DocumentSpan(document, SourceSpan);
        }
    }
}
