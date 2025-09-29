// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.FileHeaders;

namespace Microsoft.CodeAnalysis.CSharp.FileHeaders;

/// <summary>
/// Helper class used for working with file headers.
/// </summary>
internal sealed class CSharpFileHeaderHelper() : AbstractFileHeaderHelper(CSharpSyntaxKinds.Instance)
{
    public static readonly CSharpFileHeaderHelper Instance = new();

    public override string CommentPrefix => "//";

    protected override ReadOnlyMemory<char> GetTextContextOfComment(SyntaxTrivia commentTrivia)
    {
        if (commentTrivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
        {
            return commentTrivia.ToFullString().AsMemory()[2..];
        }
        else if (commentTrivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
        {
            var triviaString = commentTrivia.ToFullString();

            var startIndex = triviaString.IndexOf("/*", StringComparison.Ordinal) + 2;
            var endIndex = triviaString.LastIndexOf("*/", StringComparison.Ordinal);
            if (endIndex < startIndex)
            {
                // While editing, it is possible to have a multiline comment trivia that does not contain the closing '*/' yet.
                return triviaString.AsMemory()[startIndex..];
            }

            return triviaString.AsMemory()[startIndex..endIndex];
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(commentTrivia.Kind());
        }
    }
}
