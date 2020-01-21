// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
