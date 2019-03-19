// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    interface IToggleBlockCommentDocumentDataProvider
    {
        /// <summary>
        /// Gets the location to insert an empty comment.
        /// </summary>
        int GetEmptyCommentStartLocation(int location);

        /// <summary>
        /// Gets all block comments in a particular document.
        /// </summary>
        IEnumerable<TextSpan> GetBlockCommentsInDocument();
    }
}
