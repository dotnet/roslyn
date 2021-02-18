' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Class AlignTokensFormattingRule
        Inherits BaseFormattingRule

        Public Sub New()
        End Sub

        Public Overrides Sub AddAlignTokensOperationsSlow(operations As SegmentedList(Of AlignTokensOperation), node As SyntaxNode, ByRef nextOperation As NextAlignTokensOperationAction)
            nextOperation.Invoke()

            Dim queryExpression = TryCast(node, QueryExpressionSyntax)
            If queryExpression?.Clauses.Count >= 2 Then
                AddAlignIndentationOfTokensToBaseTokenOperation(
                    operations,
                    queryExpression,
                    queryExpression.Clauses(0).GetFirstToken(includeZeroWidth:=True),
                    queryExpression.Clauses.Skip(1).SelectAsArray(Function(q) q.GetFirstToken(includeZeroWidth:=True)))
            End If
        End Sub
    End Class
End Namespace
