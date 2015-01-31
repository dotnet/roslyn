' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.TextStructureNavigation
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.TextStructureNavigation

    <Export(GetType(ITextStructureNavigatorProvider))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    Friend Class TextStructureNavigatorProvider
        Inherits AbstractTextStructureNavigatorProvider

        <ImportingConstructor()>
        Friend Sub New(
            selectorService As ITextStructureNavigatorSelectorService,
            contentTypeService As IContentTypeRegistryService,
            waitIndicator As IWaitIndicator)
            MyBase.New(selectorService, contentTypeService, waitIndicator)
        End Sub

        Protected Overrides Function ShouldSelectEntireTriviaFromStart(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind() = SyntaxKind.CommentTrivia
        End Function

        Protected Overrides Function IsWithinNaturalLanguage(token As SyntaxToken, position As Integer) As Boolean
            Select Case token.Kind
                Case SyntaxKind.StringLiteralToken

                    ' Before the " is considered outside the string
                    If position = token.SpanStart Then
                        Return False
                    End If

                    If position = token.Span.End AndAlso token.Text.EndsWith("""") Then
                        Return False
                    End If

                    Return True

                Case SyntaxKind.CharacterLiteralToken

                    ' Before the ' is considered outside the character
                    If position = token.SpanStart Then
                        Return False
                    End If

                    Return True

                Case SyntaxKind.XmlTextLiteralToken
                    Return True
            End Select

            Return False
        End Function
    End Class
End Namespace
