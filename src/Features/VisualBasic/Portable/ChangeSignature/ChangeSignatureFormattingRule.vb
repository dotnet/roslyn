' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting

Namespace Microsoft.CodeAnalysis.VisualBasic.ChangeSignature
    Friend NotInheritable Class ChangeSignatureFormattingRule
        Inherits BaseFormattingRule

        Public Overrides Sub AddIndentBlockOperationsSlow(list As List(Of IndentBlockOperation), node As SyntaxNode, ByRef nextOperation As NextIndentBlockOperationAction)
            nextOperation.Invoke()

            If node.IsKind(SyntaxKind.ParameterList) OrElse node.IsKind(SyntaxKind.ArgumentList) Then

                AddChangeSignatureIndentOperation(list, node)
            End If
        End Sub

        Private Shared Sub AddChangeSignatureIndentOperation(list As List(Of IndentBlockOperation), node As SyntaxNode)
            If node.Parent IsNot Nothing Then
                Dim firstToken As SyntaxToken = node.GetFirstToken()
                Dim lastToken As SyntaxToken = node.GetLastToken()
                list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                    node.Parent.GetFirstToken(),
                    firstToken,
                    lastToken,
                    New TextSpan(firstToken.SpanStart, lastToken.Span.End - firstToken.SpanStart),
                    indentationDelta:=1,
                    option:=IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine))
            End If
        End Sub

        Public Overrides Function GetAdjustNewLinesOperationSlow(ByRef previousToken As SyntaxToken, ByRef currentToken As SyntaxToken, ByRef nextOperation As NextGetAdjustNewLinesOperation) As AdjustNewLinesOperation
            If previousToken.IsKind(SyntaxKind.CommaToken) AndAlso
               (previousToken.Parent.IsKind(SyntaxKind.ParameterList) OrElse previousToken.Parent.IsKind(SyntaxKind.ArgumentList)) Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            Return MyBase.GetAdjustNewLinesOperationSlow(previousToken, currentToken, nextOperation)
        End Function
    End Class
End Namespace
