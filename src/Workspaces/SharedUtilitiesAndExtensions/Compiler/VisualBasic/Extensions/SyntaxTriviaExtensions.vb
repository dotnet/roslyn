' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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

        <Extension()>
        Public Function IsPragmaDirective(trivia As SyntaxTrivia, ByRef isDisable As Boolean, ByRef isActive As Boolean, ByRef errorCodes As SeparatedSyntaxList(Of SyntaxNode)) As Boolean
            Select Case trivia.Kind()
                Case SyntaxKind.DisableWarningDirectiveTrivia
                    Dim pragmaWarning = DirectCast(trivia.GetStructure(), DisableWarningDirectiveTriviaSyntax)
                    errorCodes = pragmaWarning.ErrorCodes
                    isDisable = True
                    isActive = True
                    Return True

                Case SyntaxKind.EnableWarningDirectiveTrivia
                    Dim pragmaWarning = DirectCast(trivia.GetStructure(), EnableWarningDirectiveTriviaSyntax)
                    errorCodes = pragmaWarning.ErrorCodes
                    isDisable = False
                    isActive = True
                    Return True
            End Select

            errorCodes = Nothing
            isDisable = False
            isActive = False
            Return False
        End Function
    End Module
End Namespace
