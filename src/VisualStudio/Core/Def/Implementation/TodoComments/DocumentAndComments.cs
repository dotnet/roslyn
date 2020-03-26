// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TodoComments
{
    internal readonly struct DocumentAndComments
    {
        public readonly DocumentId DocumentId;
        public readonly ImmutableArray<TodoCommentData> Comments;

        public DocumentAndComments(DocumentId documentId, ImmutableArray<TodoCommentData> comments)
        {
            DocumentId = documentId;
            Comments = comments;
        }
    }
}
