' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace System.Runtime.Analyzers

    ''' <summary>
    ''' CA1820: Test for empty strings using string length.
    ''' <para>
    ''' Comparing strings using the <see cref="String.Length"/> property or the <see cref="String.IsNullOrEmpty"/> method is significantly faster than using <see cref="String.Equals(string)"/>.
    ''' This is because Equals executes significantly more MSIL instructions than either IsNullOrEmpty or the number of instructions executed to retrieve the Length property value and compare it to zero.
    ''' </para>
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicTestForEmptyStringsUsingStringLengthAnalyzer
        Inherits TestForEmptyStringsUsingStringLengthAnalyzer(Of SyntaxKind)

        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind)
            Get
                Return ImmutableArray.Create(SyntaxKind.InvocationExpression, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression)
            End Get
        End Property

        Protected Overrides Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            Select Case context.Node.Kind()
                Case SyntaxKind.InvocationExpression
                    AnalyzeInvocationExpression(context)
                Case Else
                    AnalyzeBinaryExpression(context)
            End Select
        End Sub

        Private Sub AnalyzeInvocationExpression(context As SyntaxNodeAnalysisContext)
            Dim node = DirectCast(context.Node, InvocationExpressionSyntax)
            If node.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso
                node.ArgumentList?.Arguments.Count > 0 Then

                Dim memberAccess = DirectCast(node.Expression, MemberAccessExpressionSyntax)
                If memberAccess.Name IsNot Nothing AndAlso IsEqualsMethod(memberAccess.Name.Identifier.ValueText) Then
                    Dim methodSymbol = TryCast(context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol, IMethodSymbol)
                    If methodSymbol IsNot Nothing AndAlso
                        IsEqualsMethod(methodSymbol.Name) AndAlso
                        methodSymbol.ContainingType.SpecialType = SpecialType.System_String AndAlso
                        HasAnEmptyStringArgument(node, context.SemanticModel, context.CancellationToken) Then

                        ReportDiagnostic(context, node.Expression)
                    End If
                End If
            End If
        End Sub

        Private Sub AnalyzeBinaryExpression(context As SyntaxNodeAnalysisContext)
            Dim node = DirectCast(context.Node, BinaryExpressionSyntax)
            Dim methodSymbol = TryCast(context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol, IMethodSymbol)
            If methodSymbol IsNot Nothing AndAlso
                methodSymbol.ContainingType.SpecialType = SpecialType.System_String AndAlso
                IsEqualityOrInequalityOperator(methodSymbol) AndAlso
                (IsEmptyString(node.Left, context.SemanticModel, context.CancellationToken) OrElse
                 IsEmptyString(node.Right, context.SemanticModel, context.CancellationToken)) Then

                ReportDiagnostic(context, node)
            End If
        End Sub

        Private Shared Function HasAnEmptyStringArgument(invocation As InvocationExpressionSyntax, model As SemanticModel, cancellationToken As CancellationToken) As Boolean
            For Each argument In invocation.ArgumentList.Arguments
                If IsEmptyString(argument.GetExpression, model, cancellationToken) Then
                    Return True
                End If
            Next

            Return False
        End Function
    End Class
End Namespace
