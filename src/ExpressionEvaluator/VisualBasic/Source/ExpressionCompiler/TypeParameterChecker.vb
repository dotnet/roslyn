' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend NotInheritable Class TypeParameterChecker
        Inherits AbstractTypeParameterChecker

        <Conditional("DEBUG")>
        Public Shared Sub Check(symbol As Symbol, acceptableTypeParameters As ImmutableArray(Of TypeParameterSymbol))
            Dim checker As New TypeParameterChecker(acceptableTypeParameters)
            checker.Visit(symbol)
        End Sub

        <Conditional("DEBUG")>
        Public Shared Sub Check(node As BoundNode, acceptableTypeParameters As ImmutableArray(Of TypeParameterSymbol))
            Dim checker As New BlockChecker(New TypeParameterChecker(acceptableTypeParameters))
            checker.Visit(node)
        End Sub

        Private Sub New(acceptableTypeParameters As ImmutableArray(Of TypeParameterSymbol))
            MyBase.New(acceptableTypeParameters.As(Of ITypeParameterSymbol))
        End Sub

        Public Overrides Function GetThisParameter(method As IMethodSymbol) As IParameterSymbol
            Dim meParameter As ParameterSymbol = Nothing
            Return If(DirectCast(method, MethodSymbol).TryGetMeParameter(meParameter), meParameter, Nothing)
        End Function

        Private Class BlockChecker
            Inherits BoundTreeWalkerWithStackGuard

            Private ReadOnly _typeParameterChecker As TypeParameterChecker

            Public Sub New(typeParameterChecker As TypeParameterChecker)
                _typeParameterChecker = typeParameterChecker
            End Sub

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                Dim expression = TryCast(node, BoundExpression)
                If expression IsNot Nothing Then
                    _typeParameterChecker.Visit(expression.ExpressionSymbol)
                End If

                Return MyBase.Visit(node)
            End Function
        End Class
    End Class
End Namespace
