' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
#If MEF Then
    <ExportFormattingRule(AlignTokensFormattingRule.Name, LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=AdjustSpaceFormattingRule.Name)>
    Friend Class AlignTokensFormattingRule
#Else
    Friend Class AlignTokensFormattingRule
#End If
        Inherits BaseFormattingRule
        Friend Const Name As String = "VisualBasic Align Tokens Formatting Rule"

        Public Sub New()
        End Sub

        Public Overrides Sub AddAlignTokensOperations(operations As List(Of AlignTokensOperation), node As SyntaxNode, optionSet As OptionSet, nextOperation As NextAction(Of AlignTokensOperation))
            nextOperation.Invoke(operations)

            Dim queryExpression = TryCast(node, QueryExpressionSyntax)
            If queryExpression IsNot Nothing Then
                Dim tokens = New List(Of SyntaxToken)()
                tokens.AddRange(queryExpression.Clauses.Select(Function(q) q.GetFirstToken(includeZeroWidth:=True)))

                If tokens.Count > 1 Then
                    AddAlignIndentationOfTokensToBaseTokenOperation(operations, queryExpression, tokens(0), tokens.Skip(1))
                End If
                Return
            End If

            Dim multiLineLambda = TryCast(node, MultiLineLambdaExpressionSyntax)
            If multiLineLambda IsNot Nothing Then
                Dim baseToken = multiLineLambda.Begin.GetFirstToken(includeZeroWidth:=True)
                Dim tokenToAlign = multiLineLambda.End.GetFirstToken(includeZeroWidth:=True)

                AddAlignIndentationOfTokensToBaseTokenOperation(operations, multiLineLambda, baseToken, SpecializedCollections.SingletonEnumerable(tokenToAlign))
                Return
            End If

            Dim labelStatement = TryCast(node, LabelStatementSyntax)
            If labelStatement IsNot Nothing Then
                Dim nextToken = labelStatement.ColonToken.GetNextToken(includeZeroWidth:=True)
                AddAlignPositionOfTokensToIndentationOperation(operations, labelStatement, SpecializedCollections.SingletonEnumerable(nextToken))
            End If
        End Sub
    End Class
End Namespace