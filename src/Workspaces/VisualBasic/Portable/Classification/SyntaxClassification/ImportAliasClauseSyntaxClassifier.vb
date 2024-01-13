' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class ImportAliasClauseSyntaxClassifier
        Inherits AbstractSyntaxClassifier

        Public Overrides ReadOnly Property SyntaxNodeTypes As ImmutableArray(Of Type) = ImmutableArray.Create(GetType(ImportAliasClauseSyntax))

        Public Overrides Sub AddClassifications(syntax As SyntaxNode, textSpan As TextSpan, semanticModel As SemanticModel, options As ClassificationOptions, result As SegmentedList(Of ClassifiedSpan), cancellationToken As CancellationToken)
            ClassifyImportAliasClauseSyntax(DirectCast(syntax, ImportAliasClauseSyntax), semanticModel, result, cancellationToken)
        End Sub

        Private Shared Sub ClassifyImportAliasClauseSyntax(
                node As ImportAliasClauseSyntax,
                semanticModel As SemanticModel,
                result As SegmentedList(Of ClassifiedSpan),
                cancellationToken As CancellationToken)

            Dim symbolInfo = semanticModel.GetTypeInfo(DirectCast(node.Parent, SimpleImportsClauseSyntax).Name, cancellationToken)
            If symbolInfo.Type IsNot Nothing Then
                Dim classification = GetClassificationForType(symbolInfo.Type)
                If classification IsNot Nothing Then
                    Dim token = node.Identifier
                    result.Add(New ClassifiedSpan(token.Span, classification))
                    Return
                End If
            End If
        End Sub
    End Class
End Namespace
