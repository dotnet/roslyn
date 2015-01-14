' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicCastReducer
        Private Class Rewriter
            Inherits AbstractExpressionRewriter

            Public Sub New(optionSet As OptionSet, cancellationToken As CancellationToken)
                MyBase.New(optionSet, cancellationToken)
            End Sub

            Public Overrides Function VisitCTypeExpression(node As CTypeExpressionSyntax) As SyntaxNode
                Return SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitCTypeExpression(node),
                    simplifier:=AddressOf SimplifyCast)
            End Function

            Public Overrides Function VisitDirectCastExpression(node As DirectCastExpressionSyntax) As SyntaxNode
                Return SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitDirectCastExpression(node),
                    simplifier:=AddressOf SimplifyCast)
            End Function

            Public Overrides Function VisitTryCastExpression(node As TryCastExpressionSyntax) As SyntaxNode
                Return SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitTryCastExpression(node),
                    simplifier:=AddressOf SimplifyCast)
            End Function

            Public Overrides Function VisitPredefinedCastExpression(node As PredefinedCastExpressionSyntax) As SyntaxNode
                Return SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitPredefinedCastExpression(node),
                    simplifier:=AddressOf SimplifyCast)
            End Function

        End Class
    End Class
End Namespace
