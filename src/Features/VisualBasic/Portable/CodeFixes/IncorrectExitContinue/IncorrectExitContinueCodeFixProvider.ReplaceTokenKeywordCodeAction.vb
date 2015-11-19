' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue
    Partial Friend Class IncorrectExitContinueCodeFixProvider
        Private Class ReplaceTokenKeywordCodeAction
            Inherits CodeAction

            Private ReadOnly _blockKind As SyntaxKind
            Private _invalidToken As SyntaxToken
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
                    Return String.Format(FeaturesResources.ChangeTo, _invalidToken.ValueText, SyntaxFacts.GetText(BlockKindToKeywordKind(_blockKind)))
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
