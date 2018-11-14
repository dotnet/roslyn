' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class EndBlockStatementSyntaxClassifier
        Inherits AbstractSyntaxClassifier

        Public Overrides ReadOnly Property SyntaxNodeTypes As ImmutableArray(Of Type) = ImmutableArray.Create(GetType(EndBlockStatementSyntax))

        Public Overrides Sub AddClassifications(workspace As Workspace, syntax As SyntaxNode, semanticModel As SemanticModel, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken)
            If (TypeOf syntax Is EndBlockStatementSyntax) Then
                Dim endBlockStatement = DirectCast(syntax, EndBlockStatementSyntax)
                If (SyntaxFacts.IsControlKeyword(endBlockStatement.BlockKeyword.Kind)) Then
                    result.Add(New ClassifiedSpan(endBlockStatement.EndKeyword.Span, ClassificationTypeNames.ControlKeyword))
                End If
            End If
        End Sub

    End Class
End Namespace
