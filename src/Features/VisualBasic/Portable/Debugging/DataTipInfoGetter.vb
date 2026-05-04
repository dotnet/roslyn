' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Debugging
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Debugging
    ' TODO: Make this class static when we add that functionality to VB.
    Friend NotInheritable Class DataTipInfoGetter
        Inherits AbstractDataTipInfoGetter(Of
            ExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax)

        Public Shared Async Function GetInfoAsync(
                document As Document,
                position As Integer,
                includeKind As Boolean,
                cancellationToken As CancellationToken) As Task(Of DebugDataTipInfo)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim token = root.FindToken(position)

            If token.IsKind(SyntaxKind.CommaToken) Then
                ' The commas in a separated syntax list consider the list's parent
                ' to be their parent, which leads to false positives below.
                Return Nothing
            End If

            If token.Parent.IsKind(SyntaxKind.ModifiedIdentifier) Then
                Return New DebugDataTipInfo(token.Span, Text:=Nothing)
            End If

            Dim expression = TryCast(token.Parent, ExpressionSyntax)
            If expression Is Nothing Then
                Return If(token.IsKind(SyntaxKind.IdentifierToken),
                    New DebugDataTipInfo(token.Span, Text:=Nothing),
                    Nothing)
            End If

            If expression.IsAnyLiteralExpression() Then
                Return Nothing
            End If

            Dim kindAndExpressionSpan = Await ComputeKindAsync(document, expression, includeKind, cancellationToken).ConfigureAwait(False)

            Dim conditionalAccess As ExpressionSyntax = Nothing
            If expression.IsRightSideOfDotOrBang() Then
                expression = DirectCast(expression.Parent, ExpressionSyntax)
                conditionalAccess = If(expression.GetRootConditionalAccessExpression(), expression)
            End If

            If expression.Parent.IsKind(SyntaxKind.InvocationExpression) Then
                expression = DirectCast(expression.Parent, ExpressionSyntax)
            End If

            Dim span = expression.Span
            If conditionalAccess IsNot Nothing Then
                ' There may not be an ExpressionSyntax corresponding to the range we want.
                ' For example, for input a?.$$B?.C we want span [|a?.B|].C.
                span = TextSpan.FromBounds(conditionalAccess.SpanStart, span.End)
            End If

            Return New DebugDataTipInfo(span, If(kindAndExpressionSpan.expressionSpan, span), Text:=Nothing, kindAndExpressionSpan.kind)
        End Function
    End Class
End Namespace
