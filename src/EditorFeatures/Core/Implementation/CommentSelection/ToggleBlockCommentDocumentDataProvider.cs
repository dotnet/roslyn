// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    /// <summary>
    /// Provides a language agnostic way to retrieve data about block comments.
    /// </summary>
    internal class ToggleBlockCommentDocumentDataProvider : IToggleBlockCommentDocumentDataProvider
    {
        private readonly ITextSnapshot _snapshot;
        private readonly CommentSelectionInfo _commentInfo;

        public ToggleBlockCommentDocumentDataProvider(ITextSnapshot textSnapshot, CommentSelectionInfo commentInfo)
        {
            _snapshot = textSnapshot;
            _commentInfo = commentInfo;
        }

        public int GetEmptyCommentStartLocation(int location)
        {
            return location;
        }

        public ImmutableArray<TextSpan> GetBlockCommentsInDocument()
        {
            var allText = _snapshot.AsText();
            var commentedSpans = new List<TextSpan>();

            var openIdx = 0;
            while ((openIdx = allText.IndexOf(_commentInfo.BlockCommentStartString, openIdx, caseSensitive: true)) >= 0)
            {
                // Retrieve the first closing marker located after the open index.
                var closeIdx = allText.IndexOf(_commentInfo.BlockCommentEndString, openIdx + _commentInfo.BlockCommentStartString.Length, caseSensitive: true);
                // If an open marker is found without a close marker, it's an unclosed comment.
                if (closeIdx < 0)
                {
                    closeIdx = allText.Length - _commentInfo.BlockCommentEndString.Length;
                }

                var blockCommentSpan = new TextSpan(openIdx, closeIdx + _commentInfo.BlockCommentEndString.Length - openIdx);
                commentedSpans.Add(blockCommentSpan);
                openIdx = closeIdx;
            }

            return commentedSpans.ToImmutableArray();
        }
    }
}
