' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.Debugging
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
    ' TODO: Make this class static when we add that functionality to VB.
    Namespace DataTipInfoGetter
        Friend Module DataTipInfoGetterModule
            Friend Async Function GetInfoAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of DebugDataTipInfo)
                Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                Dim token = root.FindToken(position)

                If token.IsKind(SyntaxKind.CommaToken) Then
                    ' The commas in a separated syntax list consider the list's parent
                    ' to be their parent, which leads to false positives below.
                    Return Nothing
                End If

                If token.Parent.IsKind(SyntaxKind.ModifiedIdentifier) Then
                    Return New DebugDataTipInfo(token.Span, text:=Nothing)
                End If

                Dim expression = TryCast(token.Parent, ExpressionSyntax)
                If expression Is Nothing Then
                    Return If(token.IsKind(SyntaxKind.IdentifierToken),
                        New DebugDataTipInfo(token.Span, text:=Nothing),
                        Nothing)
                End If

                If expression.IsAnyLiteralExpression() Then
                    Return Nothing
                End If

                Dim conditionalAccess As ExpressionSyntax = Nothing
                If expression.IsRightSideOfDotOrBang() Then
                    expression = DirectCast(expression.Parent, ExpressionSyntax)

                    Dim curr = expression
                    While True
                        curr = curr.GetCorrespondingConditionalAccessExpression()
                        If curr Is Nothing Then
                            Exit While
                        End If

                        conditionalAccess = curr
                    End While
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

                Return New DebugDataTipInfo(span, text:=Nothing)
            End Function
        End Module
    End Namespace
End Namespace
