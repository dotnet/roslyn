// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal readonly struct DocumentIdSpan
    {
        public readonly Workspace Workspace;
        public readonly DocumentId DocumentId;
        public readonly TextSpan SourceSpan;

        public DocumentIdSpan(DocumentSpan documentSpan)
        {
            Workspace = documentSpan.Document.Project.Solution.Workspace;
            DocumentId = documentSpan.Document.Id;
            SourceSpan = documentSpan.SourceSpan;
        }

        public DocumentSpan? TryRehydrate()
        {
            var document = Workspace.CurrentSolution.GetDocument(DocumentId);
            return document == null ? null : new DocumentSpan(document, SourceSpan);
        }
    }
}
