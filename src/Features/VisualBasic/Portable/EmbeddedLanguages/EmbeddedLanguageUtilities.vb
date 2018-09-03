' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages
    Friend Module EmbeddedLanguageUtilities
        Friend Sub AddComment(editor As SyntaxEditor, stringLiteral As SyntaxToken, commentContents As String)
            Dim trivia = SyntaxFactory.TriviaList(
                SyntaxFactory.CommentTrivia($"' {commentContents}"),
                SyntaxFactory.ElasticCarriageReturnLineFeed)
            Dim containingStatement = stringLiteral.Parent.GetAncestor(Of StatementSyntax)
            Dim leadingBlankLines = containingStatement.GetLeadingBlankLines()
            Dim newStatement = containingStatement.GetNodeWithoutLeadingBlankLines().
                                                   WithPrependedLeadingTrivia(leadingBlankLines.AddRange(trivia))
            editor.ReplaceNode(containingStatement, newStatement)
        End Sub
    End Module
End Namespace
