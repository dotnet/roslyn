// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    internal interface IDocumentProvider
    {
        Task<Document> GetDocumentAsync(ITextSnapshot snapshot, CancellationToken cancellationToken);
    }

    internal class DocumentProvider(IThreadingContext threadingContext) : IDocumentProvider
    {
        private readonly IThreadingContext _threadingContext = threadingContext;

        public async Task<Document> GetDocumentAsync(ITextSnapshot snapshot, CancellationToken cancellationToken)
        {
            _threadingContext.ThrowIfNotOnBackgroundThread();
            return await snapshot.AsText().GetDocumentWithFrozenPartialSemanticsAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
