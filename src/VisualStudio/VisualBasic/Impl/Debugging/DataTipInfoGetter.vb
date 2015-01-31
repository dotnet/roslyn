' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.Debugging
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Debugging

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
    ' TODO: Make this class static when we add that functionality to VB.
    Namespace DataTipInfoGetter
        Module DataTipInfoGetterModule
            Friend Async Function GetInfoAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of DebugDataTipInfo)
                Dim root = Await document.GetVisualBasicSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
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

                If expression.IsRightSideOfDotOrBang() Then
                    expression = DirectCast(expression.Parent, ExpressionSyntax)
                End If

                If expression.IsKind(SyntaxKind.InvocationExpression) Then
                    expression = DirectCast(expression, InvocationExpressionSyntax).Expression
                End If

                Return New DebugDataTipInfo(expression.Span, text:=Nothing)
            End Function
        End Module
    End Namespace
End Namespace
