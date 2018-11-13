' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers

    Friend Class OperatorOverloadSyntaxClassifier
        Inherits AbstractSyntaxClassifier

        Public Overrides ReadOnly Property SyntaxNodeTypes As ImmutableArray(Of Type) = ImmutableArray.Create(
            GetType(BinaryExpressionSyntax),
            GetType(UnaryExpressionSyntax),
            GetType(BinaryConditionalExpressionSyntax))

        Public Overrides Sub AddClassifications(
            workspace As Workspace,
            syntax As SyntaxNode,
            semanticModel As SemanticModel,
            result As ArrayBuilder(Of ClassifiedSpan),
            cancellationToken As CancellationToken)

            Dim symbolInfo = semanticModel.GetSymbolInfo(syntax, cancellationToken)
            If (TypeOf symbolInfo.Symbol Is IMethodSymbol _
                AndAlso DirectCast(symbolInfo.Symbol, IMethodSymbol).MethodKind = MethodKind.UserDefinedOperator) Then
                result.Add(New ClassifiedSpan(syntax.Span, ClassificationTypeNames.OperatorOverload))
            End If
        End Sub
    End Class
End Namespace
