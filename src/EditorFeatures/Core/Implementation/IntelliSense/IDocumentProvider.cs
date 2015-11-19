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
        Task<Document> GetDocumentAsync(ITextSnapshot snapshot, CancellationToken cancellationToken);
        Document GetOpenDocumentInCurrentContextWithChanges(ITextSnapshot snapshot);
    }

    internal class DocumentProvider : ForegroundThreadAffinitizedObject, IDocumentProvider
    {
        public Task<Document> GetDocumentAsync(ITextSnapshot snapshot, CancellationToken cancellationToken)
        {
            AssertIsBackground();
            return snapshot.AsText().GetDocumentWithFrozenPartialSemanticsAsync(cancellationToken);
        }

        public Document GetOpenDocumentInCurrentContextWithChanges(ITextSnapshot snapshot)
        {
            var text = snapshot.AsText();
            return text.GetOpenDocumentInCurrentContextWithChanges();
        }
    }
}
