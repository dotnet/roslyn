' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class ImportAliasClauseSyntaxClassifier
        Inherits AbstractSyntaxClassifier

        Public Overrides ReadOnly Property SyntaxNodeTypes As ImmutableArray(Of Type) = ImmutableArray.Create(GetType(ImportAliasClauseSyntax))

        Public Overrides Sub AddClassifications(workspace As Workspace, syntax As SyntaxNode, semanticModel As SemanticModel, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken)
            ClassifyImportAliasClauseSyntax(DirectCast(syntax, ImportAliasClauseSyntax), semanticModel, result, cancellationToken)
        End Sub

        Private Sub ClassifyImportAliasClauseSyntax(
                node As ImportAliasClauseSyntax,
                semanticModel As SemanticModel,
                result As ArrayBuilder(Of ClassifiedSpan),
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
