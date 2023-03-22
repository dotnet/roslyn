' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Indentation
    Friend Class SpecialFormattingRule
        Inherits CompatAbstractFormattingRule

        Private ReadOnly _indentStyle As FormattingOptions2.IndentStyle

        Public Sub New(indentStyle As FormattingOptions2.IndentStyle)
            _indentStyle = indentStyle
        End Sub

        Public Overrides Sub AddSuppressOperationsSlow(list As List(Of SuppressOperation), node As SyntaxNode, ByRef nextOperation As NextSuppressOperationAction)
            ' don't suppress anything
        End Sub

        Public Overrides Function GetAdjustNewLinesOperationSlow(ByRef previousToken As SyntaxToken, ByRef currentToken As SyntaxToken, ByRef nextOperation As NextGetAdjustNewLinesOperation) As AdjustNewLinesOperation

            ' unlike regular one. force position of attribute
            Dim attributeNode = TryCast(previousToken.Parent, AttributeListSyntax)
            If attributeNode IsNot Nothing AndAlso attributeNode.GreaterThanToken = previousToken Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            ' no line operation. no line changes what so ever
            Dim lineOperation = MyBase.GetAdjustNewLinesOperationSlow(previousToken, currentToken, nextOperation)
            If lineOperation IsNot Nothing Then
                ' basically means don't ever put new line if there isn't already one, but do
                ' indentation.
                Return FormattingOperations.CreateAdjustNewLinesOperation(line:=0, option:=AdjustNewLinesOption.PreserveLines)
            End If

            Return Nothing
        End Function

        Public Overrides Function GetAdjustSpacesOperationSlow(ByRef previousToken As SyntaxToken, ByRef currentToken As SyntaxToken, ByRef nextOperation As NextGetAdjustSpacesOperation) As AdjustSpacesOperation
            Dim spaceOperation = MyBase.GetAdjustSpacesOperationSlow(previousToken, currentToken, nextOperation)

            ' if there is force space operation, convert it to ForceSpaceIfSingleLine operation.
            ' (force space basically means remove all line breaks)
            If spaceOperation IsNot Nothing AndAlso spaceOperation.Option = AdjustSpacesOption.ForceSpaces Then
                Return FormattingOperations.CreateAdjustSpacesOperation(spaceOperation.Space, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            Return spaceOperation
        End Function

        Public Overrides Sub AddIndentBlockOperationsSlow(list As List(Of IndentBlockOperation), node As SyntaxNode, ByRef nextOperation As NextIndentBlockOperationAction)
            nextOperation.Invoke()

            Dim singleLineLambdaFunction = TryCast(node, SingleLineLambdaExpressionSyntax)
            If singleLineLambdaFunction IsNot Nothing Then
                Dim baseToken = node.GetFirstToken(includeZeroWidth:=True)
                Dim endToken = node.GetLastToken()
                Dim nextToken = endToken.GetNextToken(includeZeroWidth:=True)

                If SyntaxFacts.AllowsTrailingImplicitLineContinuation(endToken) OrElse
                   SyntaxFacts.AllowsLeadingImplicitLineContinuation(nextToken) OrElse
                   endToken.TrailingTrivia.Any(SyntaxKind.LineContinuationTrivia) Then
                    Dim startToken = baseToken.GetNextToken(includeZeroWidth:=True)
                    list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                                baseToken, startToken, endToken,
                                TextSpan.FromBounds(startToken.FullSpan.Start, node.FullSpan.End), indentationDelta:=1, [option]:=IndentBlockOption.RelativePosition))
                End If

                Return
            End If

            AddIndentBlockOperations(Of ParameterListSyntax)(list, node, Function(n) Not n.OpenParenToken.IsMissing AndAlso n.Parameters.Count > 0)
            AddArgumentListIndentBlockOperations(list, node)
            AddIndentBlockOperations(Of TypeParameterListSyntax)(list, node, Function(n) Not n.OpenParenToken.IsMissing AndAlso n.Parameters.Count > 0, indentationDelta:=1)
        End Sub

        Private Overloads Shared Sub AddIndentBlockOperations(Of T As SyntaxNode)(list As List(Of IndentBlockOperation), node As SyntaxNode, predicate As Func(Of T, Boolean), Optional indentationDelta As Integer = 0)
            Dim parameterOrArgumentList = TryCast(node, T)
            If parameterOrArgumentList Is Nothing Then
                Return
            End If

            If Not predicate(parameterOrArgumentList) Then
                Return
            End If

            AddIndentBlockOperations(list, parameterOrArgumentList, indentationDelta)
        End Sub

        Private Overloads Shared Sub AddIndentBlockOperations(list As List(Of IndentBlockOperation), parameterOrArgumentList As SyntaxNode, indentationDelta As Integer)
            Dim openBrace = parameterOrArgumentList.GetFirstToken(includeZeroWidth:=True)
            Dim closeBrace = parameterOrArgumentList.GetLastToken(includeZeroWidth:=True)

            ' first token of first argument (first token of the node should be "open brace")
            Dim baseToken = openBrace.GetNextToken(includeZeroWidth:=True)

            ' indent block start token
            Dim startToken = baseToken.GetNextToken(includeZeroWidth:=True)

            ' last token of last argument (last token of the node should be "close brace")
            Dim endToken = closeBrace.GetPreviousToken(includeZeroWidth:=True)

            list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                    baseToken, startToken, endToken, TextSpan.FromBounds(baseToken.Span.End, closeBrace.Span.End), indentationDelta, IndentBlockOption.RelativePosition))
        End Sub

        Private Shared Sub AddArgumentListIndentBlockOperations(operations As List(Of IndentBlockOperation), node As SyntaxNode)
            Dim argumentList = TryCast(node, ArgumentListSyntax)
            If argumentList Is Nothing OrElse
               argumentList.OpenParenToken.IsMissing OrElse
               argumentList.Arguments.Count = 0 OrElse
               argumentList.Arguments.All(Function(a) a.IsMissing) Then
                Return
            End If

            Dim openBrace = argumentList.GetFirstToken(includeZeroWidth:=True)
            Dim closeBrace = argumentList.GetLastToken(includeZeroWidth:=True)

            Dim sourceText = node.SyntaxTree.GetText()

            ' First, create a list of all arguments that start lines.
            Dim arguments = New List(Of ArgumentSyntax)

            Dim previousLineNumber = -1
            For Each argument In argumentList.Arguments
                Dim lineNumber = sourceText.Lines.GetLineFromPosition(argument.SpanStart).LineNumber
                If lineNumber > previousLineNumber Then
                    arguments.Add(argument)
                    previousLineNumber = lineNumber
                End If
            Next

            ' Next create indent block operations using the arguments that start each line.
            ' These indent block operations span to the start of the next argument starting a line,
            ' or the close brace of the argument list if there isn't another argument.
            For i = 0 To arguments.Count - 1
                Dim argument = arguments(i)
                Dim baseToken = argument.GetFirstToken(includeZeroWidth:=True)
                Dim startToken = baseToken.GetNextToken(includeZeroWidth:=True)

                Dim endToken As SyntaxToken
                Dim span As TextSpan

                If i < arguments.Count - 1 Then
                    Dim nextArgument = arguments(i + 1)
                    Dim firstToken = nextArgument.GetFirstToken(includeZeroWidth:=True)

                    endToken = firstToken.GetPreviousToken(includeZeroWidth:=True)

                    span = TextSpan.FromBounds(baseToken.Span.End, firstToken.SpanStart)
                Else
                    endToken = closeBrace.GetPreviousToken(includeZeroWidth:=True)
                    span = TextSpan.FromBounds(baseToken.Span.End, closeBrace.Span.End)
                End If

                Dim operation = FormattingOperations.CreateRelativeIndentBlockOperation(
                    baseToken, startToken, endToken, span, indentationDelta:=0, IndentBlockOption.RelativePosition)

                operations.Add(operation)
            Next
        End Sub

        Public Overrides Sub AddAlignTokensOperationsSlow(operations As List(Of AlignTokensOperation), node As SyntaxNode, ByRef nextAction As NextAlignTokensOperationAction)
            MyBase.AddAlignTokensOperationsSlow(operations, node, nextAction)

            ' Smart token formatting off: No token alignment
            If _indentStyle <> FormattingOptions2.IndentStyle.Smart Then
                Return
            End If

            AddAlignTokensOperations(Of ParameterListSyntax)(operations, node, Function(n) n.OpenParenToken)
            AddAlignTokensOperations(Of TypeParameterListSyntax)(operations, node, Function(n) n.OpenParenToken)
            AddAlignTokensOperations(Of ArrayRankSpecifierSyntax)(operations, node, Function(n) n.OpenParenToken)

            AddCaseClauseAlignTokensOperations(operations, node)
        End Sub

        Private Shared Sub AddCaseClauseAlignTokensOperations(operations As List(Of AlignTokensOperation), node As SyntaxNode)
            Dim caseStatement = TryCast(node, CaseStatementSyntax)
            If caseStatement Is Nothing OrElse caseStatement.Cases.Count = 0 Then
                Return
            End If

            Dim cases = caseStatement.Cases.OfType(Of SimpleCaseClauseSyntax).ToList()
            If cases.Count < 2 Then
                Return
            End If

            operations.Add(FormattingOperations.CreateAlignTokensOperation(
                           cases(0).GetFirstToken(includeZeroWidth:=True),
                           cases.Skip(1).Select(Function(n) n.GetFirstToken(includeZeroWidth:=True)),
                           AlignTokensOption.AlignIndentationOfTokensToBaseToken))
        End Sub

        Private Overloads Shared Sub AddAlignTokensOperations(Of T As SyntaxNode)(operations As List(Of AlignTokensOperation), node As SyntaxNode, baseTokenGetter As Func(Of T, SyntaxToken))
            Dim parameterList = TryCast(node, T)
            If parameterList Is Nothing Then
                Return
            End If

            Dim baseToken = baseTokenGetter(parameterList)
            If baseToken.IsMissing Then
                Return
            End If

            AddAlignTokensOperations(operations, baseToken)
        End Sub

        Private Overloads Shared Sub AddAlignTokensOperations(operations As List(Of AlignTokensOperation), baseToken As SyntaxToken)
            operations.Add(FormattingOperations.CreateAlignTokensOperation(
                           baseToken,
                           SpecializedCollections.SingletonEnumerable(baseToken.GetNextToken(includeZeroWidth:=True)),
                           AlignTokensOption.AlignIndentationOfTokensToBaseToken))
        End Sub
    End Class
End Namespace
