// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
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
    [DataContract]
    internal readonly struct DocumentIdSpan
    {
        [DataMember(Order = 0)]
        public readonly DocumentId DocumentId;
        [DataMember(Order = 1)]
        public readonly TextSpan SourceSpan;

        public DocumentIdSpan(DocumentId documentId, TextSpan sourceSpan)
        {
            DocumentId = documentId;
            SourceSpan = sourceSpan;
        }

        public static implicit operator DocumentIdSpan(DocumentSpan documentSpan)
            => new(documentSpan.Document.Id, documentSpan.SourceSpan);

        public async Task<DocumentSpan?> TryRehydrateAsync(Solution solution, CancellationToken cancellationToken)
        {
            var document = await solution.GetDocumentAsync(this.DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            return document == null ? null : new DocumentSpan(document, this.SourceSpan);
        }
    }
}
