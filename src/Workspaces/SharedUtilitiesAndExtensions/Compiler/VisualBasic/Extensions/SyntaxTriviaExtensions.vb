' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module SyntaxTriviaExtensions
        <Extension()>
        Public Function IsKind(trivia As SyntaxTrivia, kind1 As SyntaxKind, kind2 As SyntaxKind) As Boolean
            Return trivia.Kind = kind1 OrElse
                   trivia.Kind = kind2
        End Function

        <Extension()>
        Public Function IsKind(trivia As SyntaxTrivia, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind) As Boolean
            Return trivia.Kind = kind1 OrElse
                   trivia.Kind = kind2 OrElse
                   trivia.Kind = kind3
        End Function

        <Extension>
        Public Function IsWhitespaceOrEndOfLine(trivia As SyntaxTrivia) As Boolean
            Return trivia.IsWhitespace() OrElse trivia.IsEndOfLine()
        End Function

        <Extension>
        Public Function IsWhitespace(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind = SyntaxKind.WhitespaceTrivia
        End Function

        <Extension>
        Public Function IsEndOfLine(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind = SyntaxKind.EndOfLineTrivia
        End Function

        <Extension>
        Public Function IsRegularOrDocComment(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind = SyntaxKind.CommentTrivia OrElse trivia.Kind = SyntaxKind.DocumentationCommentTrivia
        End Function
    End Module
End Namespace
