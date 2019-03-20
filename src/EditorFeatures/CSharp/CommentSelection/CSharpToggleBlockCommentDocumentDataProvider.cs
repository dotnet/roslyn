// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CommentSelection
{
    class CSharpToggleBlockCommentDocumentDataProvider : IToggleBlockCommentDocumentDataProvider
    {
        private readonly SyntaxNode _root;

        public CSharpToggleBlockCommentDocumentDataProvider(SyntaxNode root)
        {
            _root = root;
        }

        /// <summary>
        /// Get a location of itself or the end of the token it is located in.
        /// </summary>
        public int GetEmptyCommentStartLocation(int location)
        {
            var token = _root.FindToken(location);
            if (token.Span.Contains(location))
            {
                return token.Span.End;
            }
            return location;
        }

        /// <summary>
        /// Get the location of the comments from the syntax tree.
        /// </summary>
        /// <returns></returns>
        public ImmutableArray<TextSpan> GetBlockCommentsInDocument()
        {
            return _root.DescendantTrivia()
                .Where(trivia => trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                .SelectAsArray(blockCommentTrivia => blockCommentTrivia.Span);
        }
    }
}
