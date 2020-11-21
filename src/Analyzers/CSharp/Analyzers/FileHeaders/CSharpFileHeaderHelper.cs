// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.FileHeaders;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.FileHeaders
{
    /// <summary>
    /// Helper class used for working with file headers.
    /// </summary>
    internal sealed class CSharpFileHeaderHelper : AbstractFileHeaderHelper
    {
        public static readonly CSharpFileHeaderHelper Instance = new();

        private CSharpFileHeaderHelper()
            : base(CSharpSyntaxFacts.Instance, CSharpSyntaxKinds.Instance)
        {
        }

        public override string CommentPrefix => "//";

        protected override string GetTextContextOfComment(SyntaxTrivia commentTrivia)
        {
            if (commentTrivia.MatchesKind(SyntaxKind.SingleLineCommentTrivia, SyntaxKind.MultiLineCommentTrivia))
            {
                return commentTrivia.GetCommentText();
            }

            throw ExceptionUtilities.UnexpectedValue(commentTrivia.Kind());
        }
    }
}
