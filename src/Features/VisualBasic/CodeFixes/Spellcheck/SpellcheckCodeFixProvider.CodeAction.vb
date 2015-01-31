' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Spellcheck
    Partial Friend Class SpellcheckCodeFixProvider
        Private Class SpellCheckCodeAction
            Inherits CodeAction

            Private ReadOnly complexify As Boolean
            Private ReadOnly document As Document
            Private ReadOnly originalIdentifier As SimpleNameSyntax
            Private ReadOnly newIdentifier As SimpleNameSyntax

            Sub New(document As Document, identifier As SimpleNameSyntax, replacementText As String, complexify As Boolean)
                Me.document = document
                Me.originalIdentifier = identifier
                Me.complexify = complexify

                Dim identifierToken As SyntaxToken
                If replacementText(0) = "["c Then
                    identifierToken = SyntaxFactory.BracketedIdentifier(replacementText.Substring(1, replacementText.Length - 2))
                Else
                    identifierToken = SyntaxFactory.Identifier(replacementText)
                End If

                Dim newIdentifier As SimpleNameSyntax
                Dim genericName = TryCast(identifier, GenericNameSyntax)
                If genericName IsNot Nothing Then
                    newIdentifier = genericName.WithIdentifier(identifierToken).WithLeadingTrivia(identifier.GetLeadingTrivia())
                Else
                    newIdentifier = SyntaxFactory.IdentifierName(identifierToken).WithLeadingTrivia(identifier.GetLeadingTrivia()).WithTrailingTrivia(identifier.GetTrailingTrivia())
                End If

                Me.newIdentifier = newIdentifier
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return String.Format(VBFeaturesResources.ChangeTo, originalIdentifier, newIdentifier)
                End Get
            End Property

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim annotation = New SyntaxAnnotation()

                Dim updatedDocument = Await document.ReplaceNodeAsync(
                    originalIdentifier,
                    newIdentifier.WithAdditionalAnnotations(annotation),
                    cancellationToken).ConfigureAwait(False)

                If complexify Then
                    Dim root = Await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                    Dim rootedIdentifier = root.GetAnnotatedNodes(Of SyntaxNode)(annotation).First()
                    Dim complexified = Await Simplifier.ExpandAsync(rootedIdentifier, updatedDocument, cancellationToken:=cancellationToken).ConfigureAwait(False)
                    updatedDocument = Await updatedDocument.ReplaceNodeAsync(rootedIdentifier, complexified, cancellationToken).ConfigureAwait(False)
                End If

                Return updatedDocument
            End Function
        End Class
    End Class
End Namespace

