// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    internal interface IDocumentProvider
    {
        Document GetDocument(ITextSnapshot snapshot, CancellationToken cancellationToken);
    }

    internal class DocumentProvider : IDocumentProvider
    {
        private readonly IThreadingContext _threadingContext;

        public DocumentProvider(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public Document GetDocument(ITextSnapshot snapshot, CancellationToken cancellationToken)
        {
            _threadingContext.ThrowIfNotOnBackgroundThread();
            return snapshot.AsText().GetDocumentWithFrozenPartialSemantics(cancellationToken);
        }
    }
}
