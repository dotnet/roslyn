// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.FileHeaders;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.FileHeaders
{
    /// <summary>
    /// Helper class used for working with file headers.
    /// </summary>
    internal sealed class CSharpFileHeaderHelper : AbstractFileHeaderHelper
    {
        public static readonly CSharpFileHeaderHelper Instance = new CSharpFileHeaderHelper();

        private CSharpFileHeaderHelper()
        {
        }

        public override int SingleLineCommentTriviaKind => (int)SyntaxKind.SingleLineCommentTrivia;
        public override int MultiLineCommentTriviaKind => (int)SyntaxKind.MultiLineCommentTrivia;
        public override int WhitespaceTriviaKind => (int)SyntaxKind.WhitespaceTrivia;
        public override int EndOfLineTriviaKind => (int)SyntaxKind.EndOfLineTrivia;
        public override string CommentPrefix => "//";

        protected override string GetTextContextOfComment(SyntaxTrivia commentTrivia)
        {
            if (commentTrivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                return commentTrivia.ToFullString().Substring(2);
            }
            else if (commentTrivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                var triviaString = commentTrivia.ToFullString();

                var startIndex = triviaString.IndexOf("/*", StringComparison.Ordinal) + 2;
                var endIndex = triviaString.LastIndexOf("*/", StringComparison.Ordinal);
                if (endIndex < startIndex)
                {
                    // While editing, it is possible to have a multiline comment trivia that does not contain the closing '*/' yet.
                    return triviaString.Substring(startIndex);
                }

                return triviaString.Substring(startIndex, endIndex - startIndex);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(commentTrivia.Kind());
            }
        }
    }
}
