// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    internal interface IDocumentProvider
    {
        ValueTask<Document?> GetDocumentAsync(ITextSnapshot snapshot, CancellationToken cancellationToken);
    }

    internal class DocumentProvider : ForegroundThreadAffinitizedObject, IDocumentProvider
    {
        public DocumentProvider(IThreadingContext threadingContext)
            : base(threadingContext)
        {
        }

        public ValueTask<Document?> GetDocumentAsync(ITextSnapshot snapshot, CancellationToken cancellationToken)
        {
            AssertIsBackground();
            return snapshot.AsText().GetDocumentWithFrozenPartialSemanticsAsync(cancellationToken);
        }
    }
}
