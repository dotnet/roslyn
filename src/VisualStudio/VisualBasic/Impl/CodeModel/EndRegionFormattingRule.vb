' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    Friend Class EndRegionFormattingRule
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
            Return nextOperation.Invoke()
        End Function

        Public Function GetAdjustSpacesOperation(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, nextOperation As NextOperation(Of AdjustSpacesOperation)) As AdjustSpacesOperation Implements IFormattingRule.GetAdjustSpacesOperation
            Return nextOperation.Invoke()
        End Function
    End Class
End Namespace
