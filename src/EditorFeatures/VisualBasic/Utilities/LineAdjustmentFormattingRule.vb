' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
    Friend Class LineAdjustmentFormattingRule
        Implements IFormattingRule

        Public Sub AddSuppressOperations(list As List(Of SuppressOperation), node As SyntaxNode, lastToken As SyntaxToken, optionSet As OptionSet, nextOperation As NextAction(Of SuppressOperation)) Implements IFormattingRule.AddSuppressOperations
            nextOperation.Invoke(list)
        End Sub

        Public Sub AddAnchorIndentationOperations(list As List(Of AnchorIndentationOperation), node As SyntaxNode, optionSet As OptionSet, nextOperation As NextAction(Of AnchorIndentationOperation)) Implements IFormattingRule.AddAnchorIndentationOperations
            nextOperation.Invoke(list)
        End Sub

        Public Sub AddIndentBlockOperations(list As List(Of IndentBlockOperation), node As SyntaxNode, optionSet As OptionSet, nextOperation As NextAction(Of IndentBlockOperation)) Implements IFormattingRule.AddIndentBlockOperations
            nextOperation.Invoke(list)
        End Sub

        Public Sub AddAlignTokensOperations(list As List(Of AlignTokensOperation), node As SyntaxNode, optionSet As OptionSet, nextOperation As NextAction(Of AlignTokensOperation)) Implements IFormattingRule.AddAlignTokensOperations
            nextOperation.Invoke(list)
        End Sub

        Public Function GetAdjustNewLinesOperation(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, nextOperation As NextOperation(Of AdjustNewLinesOperation)) As AdjustNewLinesOperation Implements IFormattingRule.GetAdjustNewLinesOperation
            If Not CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken) Then
                Return nextOperation.Invoke()
            End If

            Dim previous = CType(previousToken, SyntaxToken)
            Dim current = CType(currentToken, SyntaxToken)

            ' case: insert blank line in empty method body.
            If current.Kind = SyntaxKind.EndKeyword Then

                If (current.Parent.Kind = SyntaxKind.EndSubStatement AndAlso
                    current.Parent.Parent.IsKind(SyntaxKind.ConstructorBlock, SyntaxKind.SubBlock)) OrElse
                   (current.Parent.Kind = SyntaxKind.EndFunctionStatement AndAlso
                    current.Parent.Parent.Kind = SyntaxKind.FunctionBlock) AndAlso
                   Not DirectCast(current.Parent.Parent, MethodBlockSyntax).Statements.Any() Then

                    Return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines)
                End If
            End If

            ' Introduce Line operation between 2 AttributeList
            If currentToken.Kind = SyntaxKind.LessThanToken AndAlso currentToken.Parent IsNot Nothing AndAlso TypeOf currentToken.Parent Is AttributeListSyntax AndAlso
               previousToken.Kind = SyntaxKind.GreaterThanToken AndAlso previousToken.Parent IsNot Nothing AndAlso TypeOf previousToken.Parent Is AttributeListSyntax Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            Return nextOperation.Invoke()
        End Function

        Public Function GetAdjustSpacesOperation(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, nextOperation As NextOperation(Of AdjustSpacesOperation)) As AdjustSpacesOperation Implements IFormattingRule.GetAdjustSpacesOperation
            Return nextOperation.Invoke()
        End Function
    End Class
End Namespace
