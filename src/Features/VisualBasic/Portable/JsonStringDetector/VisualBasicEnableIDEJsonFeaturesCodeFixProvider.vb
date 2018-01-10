' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.JsonStringDetector
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.JsonStringDetector
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicEnableIDEJsonFeaturesCodeFixProvider)), [Shared]>
    Friend Class VisualBasicEnableIDEJsonFeaturesCodeFixProvider
        Inherits AbstractEnableIDEJsonFeaturesCodeFixProvider

        Private Shared ReadOnly s_commentTrivia As New List(Of SyntaxTrivia) From
        {
            SyntaxFactory.CommentTrivia("' lang=json"),
            SyntaxFactory.ElasticCarriageReturnLineFeed
        }

        Private Shared ReadOnly s_strictCommentTrivia As New List(Of SyntaxTrivia) From
        {
            SyntaxFactory.CommentTrivia("' lang=json,strict"),
            SyntaxFactory.ElasticCarriageReturnLineFeed
        }

        Protected Overrides Sub AddComment(editor As SyntaxEditor, stringLiteral As SyntaxToken, strict As Boolean)
            Dim containingStatement = stringLiteral.Parent.GetAncestor(Of StatementSyntax)

            Dim leadingBlankLines = containingStatement.GetLeadingBlankLines()

            Dim comment = If(strict, s_strictCommentTrivia, s_commentTrivia)
            Dim newStatement = containingStatement.GetNodeWithoutLeadingBlankLines().
                                                   WithPrependedLeadingTrivia(leadingBlankLines.AddRange(comment))

            editor.ReplaceNode(containingStatement, newStatement)
        End Sub
    End Class
End Namespace
