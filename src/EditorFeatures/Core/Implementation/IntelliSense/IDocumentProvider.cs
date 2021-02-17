// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    internal interface IDocumentProvider
    {
        Document GetDocument(ITextSnapshot snapshot, CancellationToken cancellationToken);
    }

    internal class DocumentProvider : ForegroundThreadAffinitizedObject, IDocumentProvider
    {
        public DocumentProvider(IThreadingContext threadingContext)
            : base(threadingContext)
        {
        }

        public Document GetDocument(ITextSnapshot snapshot, CancellationToken cancellationToken)
        {
            AssertIsBackground();
            return snapshot.AsText().GetDocumentWithFrozenPartialSemantics(cancellationToken);
        }
    }
}
