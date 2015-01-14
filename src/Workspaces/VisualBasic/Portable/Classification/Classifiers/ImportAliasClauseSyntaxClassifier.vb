' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class ImportAliasClauseSyntaxClassifier
        Inherits AbstractSyntaxClassifier

        Public Overrides ReadOnly Property SyntaxNodeTypes As IEnumerable(Of Type)
            Get
                Return {GetType(ImportAliasClauseSyntax)}
            End Get
        End Property

        Public Overrides Function ClassifyNode(syntax As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As IEnumerable(Of ClassifiedSpan)
            Return ClassifyImportAliasClauseSyntax(DirectCast(syntax, ImportAliasClauseSyntax), semanticModel, cancellationToken)
        End Function

        Private Function ClassifyImportAliasClauseSyntax(
                node As ImportAliasClauseSyntax,
                semanticModel As SemanticModel,
                cancellationToken As CancellationToken) As IEnumerable(Of ClassifiedSpan)

            Dim symbolInfo = semanticModel.GetTypeInfo(DirectCast(node.Parent, SimpleImportsClauseSyntax).Name, cancellationToken)
            If symbolInfo.Type IsNot Nothing Then
                Dim classification = GetClassificationForType(symbolInfo.Type)
                If classification IsNot Nothing Then
                    Dim token = node.Identifier
                    Return SpecializedCollections.SingletonEnumerable(New ClassifiedSpan(token.Span, classification))
                End If
            End If

            Return Nothing
        End Function
    End Class
End Namespace
