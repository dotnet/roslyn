﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicExtensionMethodReducer
        Inherits AbstractVisualBasicReducer

        Private Shared ReadOnly s_pool As ObjectPool(Of IReductionRewriter) =
            New ObjectPool(Of IReductionRewriter)(Function() New Rewriter(s_pool))

        Public Sub New()
            MyBase.New(s_pool)
        End Sub

        Private Shared ReadOnly s_simplifyInvocationExpression As Func(Of InvocationExpressionSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode) = AddressOf SimplifyInvocationExpression

        Private Shared Function SimplifyInvocationExpression(
            invocationExpression As InvocationExpressionSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As InvocationExpressionSyntax

            Dim rewrittenNode = invocationExpression

            If invocationExpression.Expression?.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                Dim memberAccess = DirectCast(invocationExpression.Expression, MemberAccessExpressionSyntax)
                Dim targetSymbol = semanticModel.GetSymbolInfo(memberAccess.Name)

                If (Not targetSymbol.Symbol Is Nothing) AndAlso targetSymbol.Symbol.Kind = SymbolKind.Method Then
                    Dim targetMethodSymbol = DirectCast(targetSymbol.Symbol, IMethodSymbol)
                    If Not targetMethodSymbol.IsReducedExtension() Then
                        Dim argumentList = invocationExpression.ArgumentList
                        Dim noOfArguments = argumentList.Arguments.Count

                        If noOfArguments > 0 Then
                            Dim newMemberAccess = SyntaxFactory.SimpleMemberAccessExpression(argumentList.Arguments(0).GetArgumentExpression(), memberAccess.OperatorToken, memberAccess.Name)

                            ' Below removes the first argument
                            ' we need to reuse the separators to maintain existing formatting & comments in the arguments itself
                            Dim newArguments = SyntaxFactory.SeparatedList(Of ArgumentSyntax)(argumentList.Arguments.GetWithSeparators().AsEnumerable().Skip(2))

                            Dim rewrittenArgumentList = argumentList.WithArguments(newArguments)
                            Dim candidateRewrittenNode = SyntaxFactory.InvocationExpression(newMemberAccess, rewrittenArgumentList)

                            Dim oldSymbol = semanticModel.GetSymbolInfo(invocationExpression).Symbol
                            Dim newSymbol = semanticModel.GetSpeculativeSymbolInfo(
                                invocationExpression.SpanStart,
                                candidateRewrittenNode,
                                SpeculativeBindingOption.BindAsExpression).Symbol

                            If Not oldSymbol Is Nothing And Not newSymbol Is Nothing Then
                                If newSymbol.Kind = SymbolKind.Method And oldSymbol.Equals(DirectCast(newSymbol, IMethodSymbol).GetConstructedReducedFrom()) Then
                                    rewrittenNode = candidateRewrittenNode
                                End If
                            End If
                        End If
                    End If
                End If
            End If

            Return rewrittenNode
        End Function
    End Class
End Namespace
