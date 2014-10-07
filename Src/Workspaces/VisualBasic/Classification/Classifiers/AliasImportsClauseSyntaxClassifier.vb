' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class AliasImportsClauseSyntaxClassifier
        Inherits AbstractSyntaxClassifier

        Public Overrides ReadOnly Property SyntaxNodeTypes As IEnumerable(Of Type)
            Get
                Return {GetType(AliasImportsClauseSyntax)}
            End Get
        End Property

        Public Overrides Function ClassifyNode(syntax As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As IEnumerable(Of ClassifiedSpan)
            Return ClassifyAliasImportsClauseSyntax(DirectCast(syntax, AliasImportsClauseSyntax), semanticModel, cancellationToken)
        End Function

        Private Function ClassifyAliasImportsClauseSyntax(
                node As AliasImportsClauseSyntax,
                semanticModel As SemanticModel,
                cancellationToken As CancellationToken) As IEnumerable(Of ClassifiedSpan)

            Dim symbolInfo = semanticModel.GetTypeInfo(node.Name, cancellationToken)
            If symbolInfo.Type IsNot Nothing Then
                Dim classification = GetClassificationForType(symbolInfo.Type)
                If classification IsNot Nothing Then
                    Dim token = node.Alias
                    Return SpecializedCollections.SingletonEnumerable(New ClassifiedSpan(token.Span, classification))
                End If
            End If

            Return Nothing
        End Function
    End Class
End Namespace