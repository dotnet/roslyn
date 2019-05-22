' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveDocCommentNode), [Shared]>
    Friend Class VisualBasicRemoveDocCommentNodeCodeFixProvider
        Inherits AbstractRemoveDocCommentNodeCodeFixProvider(Of XmlElementSyntax, XmlTextSyntax)

        ''' <summary>
        ''' XML comment tag with identical attributes
        ''' </summary>
        Private Const BC42305 As String = NameOf(BC42305)

        ''' <summary>
        ''' XML comment tag is not permitted on a 'sub' language element
        ''' </summary>
        Private Const BC42306 As String = NameOf(BC42306)

        ''' <summary>
        ''' XML comment type parameter does not match a type parameter
        ''' </summary>
        Private Const BC42307 As String = NameOf(BC42307)

        ''' <summary>
        ''' XML comment tag 'returns' is not permitted on a 'WriteOnly' property
        ''' </summary>
        Private Const BC42313 As String = NameOf(BC42313)

        ''' <summary>
        ''' XML comment tag 'returns' is not permitted on a 'declare sub' language element
        ''' </summary>
        Private Const BC42315 As String = NameOf(BC42315)

        ''' <summary>
        ''' XML comment type parameter does not match a type parameter
        ''' </summary>
        Private Const BC42317 As String = NameOf(BC42317)

        Friend ReadOnly Id As ImmutableArray(Of String) = ImmutableArray.Create(BC42305, BC42306, BC42307, BC42313, BC42315, BC42317)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return Id
            End Get
        End Property

        Protected Overrides ReadOnly Property DocCommentSignifierToken As String
            Get
                Return "'''"
            End Get
        End Property

        Protected Overrides Function GetRevisedDocCommentTrivia(docCommentText As String) As SyntaxTriviaList
            Return SyntaxFactory.ParseLeadingTrivia(docCommentText)
        End Function

        Protected Overrides Function GetTextTokens(xmlText As XmlTextSyntax) As SyntaxTokenList
            Return xmlText.TextTokens
        End Function

        Protected Overrides Function IsXmlWhitespaceToken(token As SyntaxToken) As Boolean
            Return token.Kind() = SyntaxKind.XmlTextLiteralToken AndAlso IsWhitespace(token.Text)
        End Function

        Protected Overrides Function IsXmlNewLineToken(token As SyntaxToken) As Boolean
            Return token.Kind() = SyntaxKind.DocumentationCommentLineBreakToken
        End Function

        Private Shared Function IsWhitespace(text As String) As Boolean
            For Each c In text
                If Not SyntaxFacts.IsWhitespace(c) Then
                    Return False
                End If
            Next

            Return True
        End Function
    End Class
End Namespace
