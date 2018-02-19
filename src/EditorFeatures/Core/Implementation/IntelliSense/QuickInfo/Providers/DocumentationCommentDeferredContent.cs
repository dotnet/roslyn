// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal class DocumentationCommentDeferredContent : IDeferredQuickInfoContent
    {
        internal string DocumentationComment { get; }

        internal void WaitForDocumentationCommentTask_ForTestingPurposesOnly()
        {
        }

        public DocumentationCommentDeferredContent(
            string documentationComment)
        {
            DocumentationComment = documentationComment;
        }

    }
}
