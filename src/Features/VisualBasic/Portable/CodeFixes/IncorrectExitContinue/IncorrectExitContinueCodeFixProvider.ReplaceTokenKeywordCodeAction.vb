' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue
    Partial Friend Class IncorrectExitContinueCodeFixProvider
        Private Class ReplaceTokenKeywordCodeAction
            Inherits CodeAction

            Private ReadOnly _blockKind As SyntaxKind
            Private ReadOnly _invalidToken As SyntaxToken
            Private ReadOnly _document As Document

            Public Sub New(blockKind As SyntaxKind,
                    invalidToken As SyntaxToken,
                    document As Document)
                Me._blockKind = blockKind
                Me._invalidToken = invalidToken
                Me._document = document
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return String.Format(FeaturesResources.Change_0_to_1, _invalidToken.ValueText, SyntaxFacts.GetText(BlockKindToKeywordKind(_blockKind)))
                End Get
            End Property

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim root = Await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                Dim rootWithoutToken = root.ReplaceToken(_invalidToken, SyntaxFactory.Token(BlockKindToKeywordKind(_blockKind)))
                Return _document.WithSyntaxRoot(rootWithoutToken)
            End Function
        End Class
    End Class
End Namespace
