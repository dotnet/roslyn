' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.SimplifyLinqExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyLinqExpression
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicSimplifyLinqExpressionDiagnosticAnalyzer
        Inherits AbstractSimplifyLinqExpressionDiagnosticAnalyzer

        Protected Overrides Function TryGetNextInvocationInChain(invocation As IInvocationOperation) As IInvocationOperation
            Return TryCast(invocation.Parent, IInvocationOperation)
        End Function

        Protected Overrides Function TryGetArgumentListLocation(arguments As ImmutableArray(Of IArgumentOperation)) As Location
            Dim argumentLists = ArrayBuilder(Of ArgumentListSyntax).GetInstance()
            Try
                For Each argument In arguments
                    Dim argumentNode = TryCast(argument.Syntax, ArgumentSyntax)
                    Dim argumentList = TryCast(argumentNode?.Parent, ArgumentListSyntax)
                    If argumentList IsNot Nothing Then
                        argumentLists.Add(argumentList)
                    End If
                Next
                If argumentLists.Count = 0 OrElse (Not argumentLists.All(Function(arglist) arglist.Equals(argumentLists(0)))) Then
                    Return Nothing
                End If

                Return argumentLists(0).GetLocation()
            Finally
                argumentLists.Free()
            End Try
        End Function

        Protected Overrides Function TryGetSymbolOfMemberAccess(invocation As IInvocationOperation) As INamedTypeSymbol
            Dim invocationNode = TryCast(invocation.Syntax, InvocationExpressionSyntax)
            Dim memberAccessExpression = TryCast(invocationNode?.Expression, MemberAccessExpressionSyntax)
            Dim expression = memberAccessExpression?.Expression
            If expression Is Nothing Then
                Return Nothing
            End If

            Dim model = invocation.SemanticModel
            Return TryCast(model.GetTypeInfo(expression).Type, INamedTypeSymbol)
        End Function

        Protected Overrides Function TryGetMethodName(invocation As IInvocationOperation) As String
            Dim invocationNode = TryCast(invocation.Syntax, InvocationExpressionSyntax)
            Dim memberAccessExpression = TryCast(invocationNode?.Expression, MemberAccessExpressionSyntax)
            Return memberAccessExpression?.Name.WithoutTrivia().GetText().ToString()
        End Function
    End Class
End Namespace
