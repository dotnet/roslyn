﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ReplacePropertyWithMethods
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ReplaceMethodWithProperty
    Partial Friend Class VisualBasicReplacePropertyWithMethods
        Inherits AbstractReplacePropertyWithMethodsService(Of IdentifierNameSyntax, ExpressionSyntax, CrefReferenceSyntax, StatementSyntax)

        Private Class ConvertValueToReturnsRewriter
            Inherits VisualBasicSyntaxRewriter

            Public Shared ReadOnly instance As New ConvertValueToReturnsRewriter()

            Private Sub New()
            End Sub

            Private Function ConvertToReturns(name As XmlNodeSyntax) As SyntaxNode
                Return name.ReplaceToken(DirectCast(name, XmlNameSyntax).LocalName,
                                         SyntaxFactory.XmlNameToken("returns", SyntaxKind.IdentifierToken))
            End Function

            Public Overrides Function VisitXmlElementStartTag(node As XmlElementStartTagSyntax) As SyntaxNode
                Return If(IsValueName(node.Name),
                          node.ReplaceNode(node.Name, ConvertToReturns(node.Name)),
                          MyBase.VisitXmlElementStartTag(node))
            End Function

            Public Overrides Function VisitXmlElementEndTag(node As XmlElementEndTagSyntax) As SyntaxNode
                Return If(IsValueName(node.Name),
                          node.ReplaceNode(node.Name, ConvertToReturns(node.Name)),
                          MyBase.VisitXmlElementEndTag(node))
            End Function
        End Class
    End Class
End Namespace
