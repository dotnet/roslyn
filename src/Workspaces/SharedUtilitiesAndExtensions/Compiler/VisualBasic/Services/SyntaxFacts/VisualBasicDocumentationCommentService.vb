' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageService
    Friend Class VisualBasicDocumentationCommentService
        Inherits AbstractDocumentationCommentService(Of
            DocumentationCommentTriviaSyntax,
            XmlNodeSyntax,
            XmlNodeSyntax,
            CrefReferenceSyntax,
            XmlElementSyntax,
            XmlTextSyntax,
            XmlEmptyElementSyntax,
            XmlCrefAttributeSyntax,
            XmlNameAttributeSyntax,
            XmlAttributeSyntax)

        Public Shared ReadOnly Instance As New VisualBasicDocumentationCommentService()

        Private Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance)
        End Sub

        Protected Overrides Function GetIdentifier(xmlName As XmlNameAttributeSyntax) As SyntaxToken
            Return xmlName.Reference.Identifier
        End Function

        Protected Overrides Function GetCref(xmlCref As XmlCrefAttributeSyntax) As CrefReferenceSyntax
            Return xmlCref.Reference
        End Function

        Protected Overrides Function GetAttributes(xmlEmpty As XmlEmptyElementSyntax) As SyntaxList(Of XmlNodeSyntax)
            Return xmlEmpty.Attributes
        End Function

        Protected Overrides Function GetTextTokens(xmlText As XmlTextSyntax) As SyntaxTokenList
            Return xmlText.TextTokens
        End Function

        Protected Overrides Function GetTextTokens(xmlTextAttribute As XmlAttributeSyntax) As SyntaxTokenList
            Dim value = TryCast(xmlTextAttribute.Value, XmlStringSyntax)
            Return If(value Is Nothing, Nothing, value.TextTokens)
        End Function

        Protected Overrides Function GetName(xmlElement As XmlElementSyntax) As SyntaxNode
            Return xmlElement.StartTag.Name
        End Function
    End Class
End Namespace
