' Copyright(c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt In the project root For license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting.FormattingOptions
Imports Microsoft.CodeAnalysis.Text

Partial Friend Class SplitCommentCommandHandler

    Private Class CommentSplitter
        Protected ReadOnly RightNodeAnnotation As SyntaxAnnotation = New SyntaxAnnotation()

        Protected ReadOnly Document As Document
        Protected ReadOnly CursorPosition As Integer
        Protected ReadOnly SourceText As SourceText
        Protected ReadOnly Root As SyntaxNode
        Protected ReadOnly TabSize As Integer
        Protected ReadOnly UseTabs As Boolean
        Protected ReadOnly CancellationToken As CancellationToken

        Private Const CommentCharacter As Char = "'"c
        Private ReadOnly _trivia As SyntaxTrivia

        Private ReadOnly _indentStyle As IndentStyle
    End Class

End Class
