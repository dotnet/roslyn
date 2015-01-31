' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue
    Partial Friend Class IncorrectExitContinueCodeFixProvider
        Private Class ReplaceTokenKeywordCodeAction
            Inherits CodeAction

            Private blockKind As SyntaxKind
            Private invalidToken As SyntaxToken
            Private document As Document

            Sub New(blockKind As SyntaxKind,
                    invalidToken As SyntaxToken,
                    document As Document)
                Me.blockKind = blockKind
                Me.invalidToken = invalidToken
                Me.document = document
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return String.Format(VBFeaturesResources.ChangeTo, invalidToken.ValueText, SyntaxFacts.GetText(BlockKindToKeywordKind(blockKind)))
                End Get
            End Property

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                Dim rootWithoutToken = root.ReplaceToken(invalidToken, SyntaxFactory.Token(BlockKindToKeywordKind(blockKind)))
                Return document.WithSyntaxRoot(rootWithoutToken)
            End Function
        End Class
    End Class
End Namespace
