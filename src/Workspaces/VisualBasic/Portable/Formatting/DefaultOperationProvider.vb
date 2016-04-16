' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    ' the default provider that will be called by the engine at the end of provider's chain.
    ' there is no way for a user to be remove this provider.
    '
    ' to reduce number of unnecessary heap allocations, most of them just return null.
    Friend Class DefaultOperationProvider
        Implements IFormattingRule

        Public Sub New()
        End Sub

        Public Sub AddSuppressOperations(operations As List(Of SuppressOperation), node As SyntaxNode, lastToken As SyntaxToken, optionSet As OptionSet, nextAction As NextAction(Of SuppressOperation)) Implements IFormattingRule.AddSuppressOperations
        End Sub

        Public Sub AddAnchorIndentationOperations(operations As List(Of AnchorIndentationOperation), node As SyntaxNode, optionSet As OptionSet, nextAction As NextAction(Of AnchorIndentationOperation)) Implements IFormattingRule.AddAnchorIndentationOperations
        End Sub

        Public Sub AddIndentBlockOperations(operations As List(Of IndentBlockOperation), node As SyntaxNode, optionSet As OptionSet, nextAction As NextAction(Of IndentBlockOperation)) Implements IFormattingRule.AddIndentBlockOperations
        End Sub

        Public Sub AddAlignTokensOperations(operations As List(Of AlignTokensOperation), node As SyntaxNode, optionSet As OptionSet, nextAction As NextAction(Of AlignTokensOperation)) Implements IFormattingRule.AddAlignTokensOperations
        End Sub

        Public Function GetAdjustNewLinesOperation(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, nextOperation As NextOperation(Of AdjustNewLinesOperation)) As AdjustNewLinesOperation Implements IFormattingRule.GetAdjustNewLinesOperation
            If previousToken.Parent Is Nothing Then
                Return Nothing
            End If

            Dim combinedTrivia = previousToken.TrailingTrivia.Concat(currentToken.LeadingTrivia)

            If ColonTriviaFollowedByLineContinuation(combinedTrivia) Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            Dim lastTrivia = combinedTrivia.LastOrDefault(AddressOf ColonOrLineContinuationTrivia)
            If lastTrivia.RawKind = SyntaxKind.ColonTrivia Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            ElseIf lastTrivia.RawKind = SyntaxKind.LineContinuationTrivia AndAlso previousToken.Parent.GetAncestorsOrThis(Of SyntaxNode)().Any(AddressOf IsSingleLineIfOrElseClauseSyntax) Then
                Return Nothing
            End If

            ' return line break operation after statement terminator token so that we can enforce indentation for the line
            If previousToken.IsLastTokenOfStatement() AndAlso ContainEndOfLine(previousToken, currentToken) AndAlso currentToken.Kind <> SyntaxKind.EmptyToken Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines)
            End If

            If previousToken.Kind = SyntaxKind.GreaterThanToken AndAlso previousToken.Parent IsNot Nothing AndAlso TypeOf previousToken.Parent Is AttributeListSyntax Then

                ' This AttributeList is the last applied attribute
                ' If this AttributeList belongs to a parameter then apply no line operation
                If previousToken.Parent.Parent IsNot Nothing AndAlso TypeOf previousToken.Parent.Parent Is ParameterSyntax Then
                    Return Nothing
                End If

                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            If currentToken.Kind = SyntaxKind.LessThanToken AndAlso currentToken.Parent IsNot Nothing AndAlso TypeOf currentToken.Parent Is AttributeListSyntax Then

                ' The case of the previousToken belonging to another AttributeList is handled in the previous condition
                If (previousToken.Kind = SyntaxKind.CommaToken OrElse previousToken.Kind = SyntaxKind.OpenParenToken) AndAlso
                   currentToken.Parent.Parent IsNot Nothing AndAlso TypeOf currentToken.Parent.Parent Is ParameterSyntax Then
                    Return Nothing
                End If

                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            ' return line break operation after xml tag token so that we can enforce indentation for the xml tag
            ' the very first xml literal tag case
            If IsFirstXmlTag(currentToken) Then
                Return Nothing
            End If

            Dim xmlDeclaration = TryCast(previousToken.Parent, XmlDeclarationSyntax)
            If xmlDeclaration IsNot Nothing AndAlso xmlDeclaration.GetLastToken(includeZeroWidth:=True) = previousToken Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            If TypeOf previousToken.Parent Is XmlNodeSyntax OrElse TypeOf currentToken.Parent Is XmlNodeSyntax Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            Return Nothing
        End Function

        Private Function IsSingleLineIfOrElseClauseSyntax(node As SyntaxNode) As Boolean
            Return TypeOf node Is SingleLineIfStatementSyntax OrElse TypeOf node Is SingleLineElseClauseSyntax
        End Function

        Private Function ColonOrLineContinuationTrivia(trivia As SyntaxTrivia) As Boolean
            Return trivia.RawKind = SyntaxKind.ColonTrivia OrElse trivia.RawKind = SyntaxKind.LineContinuationTrivia
        End Function

        Private Function ColonTriviaFollowedByLineContinuation(triviaList As IEnumerable(Of SyntaxTrivia)) As Boolean
            Dim previousTrivia As SyntaxTrivia = Nothing
            For Each trivia In triviaList
                If previousTrivia.RawKind = SyntaxKind.None OrElse trivia.RawKind = SyntaxKind.None Then
                    Continue For
                End If

                If previousTrivia.RawKind = SyntaxKind.ColonTrivia AndAlso trivia.RawKind = SyntaxKind.LineContinuationTrivia Then
                    Return True
                End If
            Next

            Return False
        End Function

        Private Function ContainEndOfLine(previousToken As SyntaxToken, nextToken As SyntaxToken) As Boolean
            Return previousToken.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia) OrElse nextToken.LeadingTrivia.Any(SyntaxKind.EndOfLineTrivia)
        End Function

        Private Function IsFirstXmlTag(currentToken As SyntaxToken) As Boolean
            Dim xmlDeclaration = TryCast(currentToken.Parent, XmlDeclarationSyntax)
            If xmlDeclaration IsNot Nothing AndAlso
               xmlDeclaration.LessThanQuestionToken = currentToken AndAlso
               TypeOf xmlDeclaration.Parent Is XmlDocumentSyntax AndAlso
               Not TypeOf xmlDeclaration.Parent.Parent Is XmlNodeSyntax Then
                Return True
            End If

            Dim startTag = TryCast(currentToken.Parent, XmlElementStartTagSyntax)
            If startTag IsNot Nothing AndAlso
               startTag.LessThanToken = currentToken AndAlso
               TypeOf startTag.Parent Is XmlElementSyntax AndAlso
               Not TypeOf startTag.Parent.Parent Is XmlNodeSyntax Then
                Return True
            End If

            Dim emptyTag = TryCast(currentToken.Parent, XmlEmptyElementSyntax)
            If emptyTag IsNot Nothing AndAlso
               emptyTag.LessThanToken = currentToken AndAlso
               Not TypeOf emptyTag.Parent Is XmlNodeSyntax Then
                Return True
            End If

            Return False
        End Function

        ' return 1 space for every token pairs as a default operation
        Public Function GetAdjustSpacesOperation(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, nextOperation As NextOperation(Of AdjustSpacesOperation)) As AdjustSpacesOperation Implements IFormattingRule.GetAdjustSpacesOperation
            If previousToken.Kind = SyntaxKind.ColonToken AndAlso
               TypeOf previousToken.Parent Is LabelStatementSyntax AndAlso
               currentToken.Kind <> SyntaxKind.EndOfFileToken Then
                Return FormattingOperations.CreateAdjustSpacesOperation(1, AdjustSpacesOption.DynamicSpaceToIndentationIfOnSingleLine)
            End If

            Dim space As Integer = If(currentToken.Kind = SyntaxKind.EndOfFileToken, 0, 1)
            Return FormattingOperations.CreateAdjustSpacesOperation(space, AdjustSpacesOption.DefaultSpacesIfOnSingleLine)
        End Function
    End Class
End Namespace
