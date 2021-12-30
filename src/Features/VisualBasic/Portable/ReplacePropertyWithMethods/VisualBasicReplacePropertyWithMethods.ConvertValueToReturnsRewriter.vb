' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ReplacePropertyWithMethods
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ReplaceMethodWithProperty
    Partial Friend Class VisualBasicReplacePropertyWithMethods
        Inherits AbstractReplacePropertyWithMethodsService(Of IdentifierNameSyntax, ExpressionSyntax, CrefReferenceSyntax, StatementSyntax, PropertyStatementSyntax)

        Private Class ConvertValueToReturnsRewriter
            Inherits VisualBasicSyntaxRewriter

            Public Shared ReadOnly instance As New ConvertValueToReturnsRewriter()

            Private Sub New()
            End Sub

            Private Shared Function ConvertToReturns(name As XmlNodeSyntax) As SyntaxNode
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
