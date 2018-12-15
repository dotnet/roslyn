' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers

    Friend Class OperatorOverloadSyntaxClassifier
        Inherits AbstractSyntaxClassifier

        Public Overrides ReadOnly Property SyntaxNodeTypes As ImmutableArray(Of Type) = ImmutableArray.Create(
            GetType(BinaryExpressionSyntax),
            GetType(UnaryExpressionSyntax))

        Public Overrides Sub AddClassifications(
            workspace As Workspace,
            syntax As SyntaxNode,
            semanticModel As SemanticModel,
            result As ArrayBuilder(Of ClassifiedSpan),
            cancellationToken As CancellationToken)

            Dim symbolInfo = semanticModel.GetSymbolInfo(syntax, cancellationToken)
            If TypeOf symbolInfo.Symbol Is IMethodSymbol AndAlso
                DirectCast(symbolInfo.Symbol, IMethodSymbol).MethodKind = MethodKind.UserDefinedOperator Then

                If TypeOf syntax Is BinaryExpressionSyntax Then
                    result.Add(New ClassifiedSpan(DirectCast(syntax, BinaryExpressionSyntax).OperatorToken.Span, ClassificationTypeNames.OperatorOverloaded))
                ElseIf TypeOf syntax Is UnaryExpressionSyntax Then
                    result.Add(New ClassifiedSpan(DirectCast(syntax, UnaryExpressionSyntax).OperatorToken.Span, ClassificationTypeNames.OperatorOverloaded))
                End If
            End If
        End Sub
    End Class
End Namespace
