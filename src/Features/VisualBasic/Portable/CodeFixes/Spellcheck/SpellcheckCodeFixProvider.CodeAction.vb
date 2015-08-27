' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Spellcheck
    Partial Friend Class SpellcheckCodeFixProvider
        Private Class SpellCheckCodeAction
            Inherits CodeAction

            Private ReadOnly _document As Document
            Private ReadOnly _originalIdentifier As SimpleNameSyntax
            Private ReadOnly _newIdentifier As SimpleNameSyntax

            Public Sub New(document As Document, identifier As SimpleNameSyntax, replacementText As String)
                Me._document = document
                Me._originalIdentifier = identifier

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

                Me._newIdentifier = newIdentifier
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return String.Format(VBFeaturesResources.ChangeTo, _originalIdentifier, _newIdentifier)
                End Get
            End Property

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim annotation = New SyntaxAnnotation()

                Dim updatedDocument = Await _document.ReplaceNodeAsync(
                    _originalIdentifier,
                    _newIdentifier.WithAdditionalAnnotations(annotation),
                    cancellationToken).ConfigureAwait(False)

                Dim semanticModel = Await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

                If ShouldComplexify(_newIdentifier.Identifier.ValueText, semanticModel, _originalIdentifier.SpanStart) Then
                    Dim root = Await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                    Dim rootedIdentifier = root.GetAnnotatedNodes(Of SyntaxNode)(annotation).First()
                    Dim complexified = Await Simplifier.ExpandAsync(rootedIdentifier, updatedDocument, cancellationToken:=cancellationToken).ConfigureAwait(False)

                    updatedDocument = Await updatedDocument.ReplaceNodeAsync(rootedIdentifier, complexified, cancellationToken).ConfigureAwait(False)
                End If

                Return updatedDocument
            End Function

            Private Shared Function ShouldComplexify(item As String, semanticModel As SemanticModel, position As Integer) As Boolean
                ' If it's not a predefined type name, we should try to complexify
                Dim type = semanticModel.GetSpeculativeTypeInfo(position, SyntaxFactory.ParseExpression(item), SpeculativeBindingOption.BindAsTypeOrNamespace).Type

                Return type IsNot Nothing AndAlso Not IsPredefinedType(type)
            End Function

            Private Shared Function IsPredefinedType(type As ITypeSymbol) As Boolean
                Select Case type.SpecialType
                    Case SpecialType.System_Boolean,
                     SpecialType.System_Byte,
                     SpecialType.System_SByte,
                     SpecialType.System_Int16,
                     SpecialType.System_UInt16,
                     SpecialType.System_Int32,
                     SpecialType.System_UInt32,
                     SpecialType.System_Int64,
                     SpecialType.System_UInt64,
                     SpecialType.System_Single,
                     SpecialType.System_Double,
                     SpecialType.System_Decimal,
                     SpecialType.System_DateTime,
                     SpecialType.System_Char,
                     SpecialType.System_String,
                     SpecialType.System_Enum,
                     SpecialType.System_Object,
                     SpecialType.System_Delegate
                        Return True
                    Case Else
                        Return False
                End Select
            End Function

        End Class
    End Class
End Namespace

