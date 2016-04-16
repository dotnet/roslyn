' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting

Namespace Microsoft.CodeAnalysis.VisualBasic.ChangeSignature
    Friend NotInheritable Class ChangeSignatureFormattingRule
        Inherits BaseFormattingRule

        Public Overrides Sub AddIndentBlockOperations(list As List(Of IndentBlockOperation), node As SyntaxNode, optionSet As OptionSet, nextOperation As NextAction(Of IndentBlockOperation))
            nextOperation.Invoke(list)

            If node.IsKind(SyntaxKind.ParameterList) OrElse node.IsKind(SyntaxKind.ArgumentList) Then

                AddChangeSignatureIndentOperation(list, node)
            End If
        End Sub

        Private Sub AddChangeSignatureIndentOperation(list As List(Of IndentBlockOperation), node As SyntaxNode)
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

        Public Overrides Function GetAdjustNewLinesOperation(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, nextOperation As NextOperation(Of AdjustNewLinesOperation)) As AdjustNewLinesOperation
            If previousToken.IsKind(SyntaxKind.CommaToken) AndAlso
               (previousToken.Parent.IsKind(SyntaxKind.ParameterList) OrElse previousToken.Parent.IsKind(SyntaxKind.ArgumentList)) Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            Return MyBase.GetAdjustNewLinesOperation(previousToken, currentToken, optionSet, nextOperation)
        End Function
    End Class
End Namespace
