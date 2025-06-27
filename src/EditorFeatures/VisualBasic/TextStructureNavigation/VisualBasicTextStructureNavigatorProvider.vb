' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.TextStructureNavigation
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.TextStructureNavigation
    <Export(GetType(ITextStructureNavigatorProvider))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    Friend NotInheritable Class VisualBasicTextStructureNavigatorProvider
        Inherits AbstractTextStructureNavigatorProvider

        <ImportingConstructor()>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
            selectorService As ITextStructureNavigatorSelectorService,
            contentTypeService As IContentTypeRegistryService,
            uiThreadOperationExecutor As IUIThreadOperationExecutor)
            MyBase.New(selectorService, contentTypeService, uiThreadOperationExecutor)
        End Sub

        Protected Overrides Function ShouldSelectEntireTriviaFromStart(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind() = SyntaxKind.CommentTrivia
        End Function

        Private Shared Function IsWithinNaturalLanguage(token As SyntaxToken, position As Integer) As Boolean
            Select Case token.Kind
                Case SyntaxKind.StringLiteralToken
                    ' This, in combination with the override of GetExtentOfWordFromToken() below, treats the closing
                    ' quote as a separate token.  This maintains behavior with VS2013.
                    If position = token.Span.End - 1 AndAlso token.Text.EndsWith("""", StringComparison.Ordinal) Then
                        Return False
                    End If

                    Return True

                Case SyntaxKind.CharacterLiteralToken
                    ' Before the opening quote is considered outside the character
                    If position = token.SpanStart Then
                        Return False
                    End If

                    Return True

                Case SyntaxKind.InterpolatedStringTextToken,
                     SyntaxKind.XmlTextLiteralToken
                    Return True
            End Select

            Return False
        End Function

        Protected Overrides Function GetExtentOfWordFromToken(navigator As ITextStructureNavigator, token As SyntaxToken, position As SnapshotPoint) As TextExtent
            If IsWithinNaturalLanguage(token, position) Then
                ' Defer to the editor to determine this.
                Return navigator.GetExtentOfWord(position)
            End If

            If token.Kind() = SyntaxKind.StringLiteralToken AndAlso position.Position = token.Span.End - 1 AndAlso token.Text.EndsWith("""", StringComparison.Ordinal) Then
                ' Special case to treat the closing quote of a string literal as a separate token.  This allows the
                ' cursor to stop during word navigation (Ctrl+LeftArrow, etc.) immediately before AND after the
                ' closing quote, just like it did in VS2013 and like it currently does for interpolated strings.
                Dim span = New Span(position.Position, 1)
                Return New TextExtent(New SnapshotSpan(position.Snapshot, Span), isSignificant:=True)
            Else
                Return GetTokenExtent(token, position.Snapshot)
            End If
        End Function
    End Class
End Namespace
