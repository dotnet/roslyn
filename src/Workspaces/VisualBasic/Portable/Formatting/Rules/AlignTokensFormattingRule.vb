' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Class AlignTokensFormattingRule
        Inherits BaseFormattingRule
        Friend Const Name As String = "VisualBasic Align Tokens Formatting Rule"

        Public Sub New()
        End Sub

        Public Overrides Sub AddAlignTokensOperationsSlow(operations As List(Of AlignTokensOperation), node As SyntaxNode, ByRef nextOperation As NextAlignTokensOperationAction)
            nextOperation.Invoke()

            Dim queryExpression = TryCast(node, QueryExpressionSyntax)
            If queryExpression IsNot Nothing Then
                Dim tokens = New List(Of SyntaxToken)()
                tokens.AddRange(queryExpression.Clauses.Select(Function(q) q.GetFirstToken(includeZeroWidth:=True)))

                If tokens.Count > 1 Then
                    AddAlignIndentationOfTokensToBaseTokenOperation(operations, queryExpression, tokens(0), tokens.Skip(1))
                End If

                Return
            End If
        End Sub
    End Class
End Namespace
