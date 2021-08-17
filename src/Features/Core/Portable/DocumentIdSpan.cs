// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Similar to <see cref="DocumentSpan"/> but can be held without rooting a particular <see cref="Solution"/>
    /// snapshot indefinitely.
    /// </summary>
    internal readonly struct DocumentIdSpan
    {
        public Workspace Workspace { get; }
        public DocumentId DocumentId { get; }
        public TextSpan SourceSpan { get; }

        public DocumentIdSpan(DocumentSpan documentSpan)
            : this(documentSpan.Document, documentSpan.SourceSpan)
        {
        }

        public DocumentIdSpan(Document document, TextSpan sourceSpan)
            : this(document.Project.Solution.Workspace, document.Id, sourceSpan)
        {
        }

        public DocumentIdSpan(Workspace workspace, DocumentId documentId, TextSpan sourceSpan)
        {
            Workspace = workspace;
            DocumentId = documentId;
            SourceSpan = sourceSpan;
        }

        public async Task<DocumentSpan?> TryRehydrateAsync(CancellationToken cancellationToken)
        {
            var solution = Workspace.CurrentSolution;
            var document = solution.GetDocument(DocumentId) ??
                           await solution.GetSourceGeneratedDocumentAsync(DocumentId, cancellationToken).ConfigureAwait(false);
            return document == null ? null : new DocumentSpan(document, SourceSpan);
        }
    }
}
