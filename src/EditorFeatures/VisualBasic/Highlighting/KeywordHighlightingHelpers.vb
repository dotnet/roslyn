' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    Friend Module KeywordHighlightingHelpers

        Private Iterator Function HighlightRelatedStatements(Of T As SyntaxNode)(
                                                                                  thisnode As SyntaxNode,
                                                                                  blockKind As SyntaxKind,
                                                                                  checkReturns As Boolean
                                                                                ) As IEnumerable(Of TextSpan)
            Dim nodes As New LinkedList(Of SyntaxNode)
            nodes.AddFirst(thisnode)
            While nodes.Count > 0
                Dim node = nodes(0)
                nodes.RemoveFirst()

                If blockKind <> SyntaxKind.None AndAlso TypeOf node Is ExitStatementSyntax Then
                    With DirectCast(node, ExitStatementSyntax)
                        If .BlockKeyword.Kind = blockKind Then Yield .Span
                    End With
                ElseIf blockKind <> SyntaxKind.None AndAlso TypeOf node Is ContinueStatementSyntax Then
                    With DirectCast(node, ContinueStatementSyntax)
                        If .BlockKeyword.Kind = blockKind Then Yield .Span
                    End With
                ElseIf checkReturns AndAlso TypeOf node Is ReturnStatementSyntax Then
                    With DirectCast(node, ReturnStatementSyntax)
                        Yield .ReturnKeyword.Span
                    End With
                Else
                    Dim children = node.ChildNodes.Where(Function(child) TypeOf child IsNot T AndAlso TypeOf child IsNot LambdaExpressionSyntax)
                    nodes.AddRangeAtHead(children)
                End If
            End While
        End Function

        <Extension()>
        Friend Function GetRelatedStatementHighlights(Of T As SyntaxNode)(
                                                                           node As T,
                                                                           blockKind As SyntaxKind,
                                                                  Optional checkReturns As Boolean = False
                                                                         ) As IEnumerable(Of TextSpan)

            Return HighlightRelatedStatements(Of T)(node, blockKind, checkReturns)
        End Function

        <Extension()>
        Friend Function IsIncorrectContinueStatement(node As SyntaxNode, expectedKind As SyntaxKind) As Boolean
            Dim continueStatement = TryCast(node, ContinueStatementSyntax)
            Return (continueStatement IsNot Nothing) AndAlso (continueStatement.Kind <> expectedKind)
        End Function

        <Extension()>
        Friend Function IsIncorrectExitStatement(node As SyntaxNode, expectedKind As SyntaxKind) As Boolean
            Dim exitStatement = TryCast(node, ExitStatementSyntax)
            Return (exitStatement IsNot Nothing) AndAlso (exitStatement.Kind <> expectedKind)
        End Function

        <Extension>
        Friend Iterator Function HighlightRelatedAwaits(thisnode As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return

            Dim nodes As New LinkedList(Of SyntaxNode)
            nodes.AddFirst(thisnode)
            While nodes.Count > 0

                Dim node = nodes(0) : nodes.RemoveFirst()

                If cancellationToken.IsCancellationRequested Then Continue While

                If TypeOf node Is AwaitExpressionSyntax Then
                    With DirectCast(node, AwaitExpressionSyntax)
                        Yield .AwaitKeyword.Span
                    End With
                End If

                Dim children = node.ChildNodes.Where(Function(child) TypeOf child IsNot LambdaExpressionSyntax)
                nodes.AddRangeAtHead(children)

            End While
        End Function

        Private Iterator Function HighlightRelatedYieldStatements(Of T)(thisnode As SyntaxNode) As IEnumerable(Of TextSpan)
            Dim nodes As New LinkedList(Of SyntaxNode)
            nodes.AddFirst(thisnode)
            While nodes.Count > 0

                Dim node = nodes(0) : nodes.RemoveFirst()

                If TypeOf node Is YieldStatementSyntax Then
                    With DirectCast(node, YieldStatementSyntax)
                        Yield .YieldKeyword.Span
                    End With
                Else
                    Dim children = node.ChildNodes().Where(Function(child) TypeOf child IsNot LambdaExpressionSyntax)
                    nodes.AddRangeAtHead(children)
                End If
            End While
        End Function

        <Extension>
        Friend Function GetRelatedYieldStatementHighlights(Of T As SyntaxNode)(node As T) As IEnumerable(Of TextSpan)
            Return HighlightRelatedYieldStatements(Of T)(node)
        End Function

    End Module
End Namespace
