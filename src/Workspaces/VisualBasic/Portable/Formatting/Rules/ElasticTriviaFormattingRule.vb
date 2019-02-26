' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Class ElasticTriviaFormattingRule
        Inherits BaseFormattingRule
        Friend Const Name As String = "VisualBasic Elastic Trivia Formatting Rule"

        Public Sub New()
        End Sub

        Public Overrides Sub AddSuppressOperationsSlow(list As List(Of SuppressOperation), node As SyntaxNode, optionSet As OptionSet, ByRef nextOperation As NextSuppressOperationAction)
            nextOperation.Invoke()
        End Sub

        Public Overrides Sub AddIndentBlockOperationsSlow(list As List(Of IndentBlockOperation), node As SyntaxNode, optionSet As OptionSet, ByRef nextOperation As NextIndentBlockOperationAction)
            nextOperation.Invoke()

            If node.Kind = SyntaxKind.ObjectMemberInitializer Then
                Dim initializer = DirectCast(node, ObjectMemberInitializerSyntax)

                If initializer.GetLeadingTrivia().HasAnyWhitespaceElasticTrivia() Then
                    AddIndentBlockOperation(list,
                                            initializer.OpenBraceToken,
                                            initializer.CloseBraceToken.GetPreviousToken(),
                                            [option]:=IndentBlockOption.RelativePosition)

                    list.Add(FormattingOperations.CreateIndentBlockOperation(
                             initializer.CloseBraceToken, initializer.CloseBraceToken,
                             indentationDelta:=0,
                             [option]:=IndentBlockOption.RelativePosition))
                End If
            End If

            If node.Kind = SyntaxKind.ObjectCollectionInitializer Then
                Dim collectionInitializer = DirectCast(node, ObjectCollectionInitializerSyntax)

                If collectionInitializer.GetLeadingTrivia().HasAnyWhitespaceElasticTrivia() Then
                    Dim initializer = collectionInitializer.Initializer

                    AddIndentBlockOperation(list,
                                            initializer.OpenBraceToken,
                                            initializer.CloseBraceToken.GetPreviousToken(),
                                            [option]:=IndentBlockOption.RelativePosition)

                    list.Add(FormattingOperations.CreateIndentBlockOperation(
                             initializer.CloseBraceToken, initializer.CloseBraceToken,
                             indentationDelta:=0,
                             [option]:=IndentBlockOption.RelativePosition))
                End If
            End If
        End Sub

        Public Overrides Sub AddAlignTokensOperationsSlow(list As List(Of AlignTokensOperation),
                                                      node As SyntaxNode,
                                                      optionSet As OptionSet,
                                                      ByRef nextOperation As NextAlignTokensOperationAction)
            nextOperation.Invoke()

            If node.Kind = SyntaxKind.ObjectMemberInitializer Then
                Dim initializer = DirectCast(node, ObjectMemberInitializerSyntax)

                If initializer.GetLeadingTrivia().HasAnyWhitespaceElasticTrivia() Then
                    list.Add(New AlignTokensOperation(
                             initializer.WithKeyword,
                             SpecializedCollections.SingletonEnumerable(initializer.CloseBraceToken),
                             [option]:=AlignTokensOption.AlignIndentationOfTokensToFirstTokenOfBaseTokenLine))
                End If
            End If

            If node.Kind = SyntaxKind.ObjectCollectionInitializer Then
                Dim collectionInitializer = DirectCast(node, ObjectCollectionInitializerSyntax)

                If collectionInitializer.GetLeadingTrivia().HasAnyWhitespaceElasticTrivia() Then
                    list.Add(New AlignTokensOperation(
                             collectionInitializer.FromKeyword,
                             SpecializedCollections.SingletonEnumerable(collectionInitializer.Initializer.CloseBraceToken),
                             [option]:=AlignTokensOption.AlignIndentationOfTokensToFirstTokenOfBaseTokenLine))
                End If
            End If
        End Sub

        Public Overrides Function GetAdjustSpacesOperationSlow(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, ByRef nextOperation As NextGetAdjustSpacesOperation) As AdjustSpacesOperation
            ' if it doesn't have elastic trivia, pass it through
            If Not CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken) Then
                Return nextOperation.Invoke()
            End If

            ' if it has one, check whether there is a forced one
            Dim operation = nextOperation.Invoke()

            If operation IsNot Nothing AndAlso operation.Option = AdjustSpacesOption.ForceSpaces Then
                Return operation
            End If

            ' remove blank lines between parameter lists and implements clauses
            If currentToken.Kind = SyntaxKind.ImplementsKeyword AndAlso
               (previousToken.GetAncestor(Of MethodStatementSyntax)() IsNot Nothing OrElse
                previousToken.GetAncestor(Of PropertyStatementSyntax)() IsNot Nothing OrElse
                previousToken.GetAncestor(Of EventStatementSyntax)() IsNot Nothing) Then
                Return FormattingOperations.CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces)
            End If

            ' handle comma separated lists in implements clauses
            If previousToken.GetAncestor(Of ImplementsClauseSyntax)() IsNot Nothing AndAlso currentToken.Kind = SyntaxKind.CommaToken Then
                Return FormattingOperations.CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)
            End If

            If currentToken.Kind = SyntaxKind.OpenBraceToken AndAlso
               currentToken.Parent.Kind = SyntaxKind.CollectionInitializer AndAlso
               currentToken.Parent.Parent.Kind = SyntaxKind.ObjectCollectionInitializer Then
                Return New AdjustSpacesOperation(1,
                        [option]:=AdjustSpacesOption.ForceSpaces)
            End If

            Return operation
        End Function

        Public Overrides Function GetAdjustNewLinesOperationSlow(
                previousToken As SyntaxToken,
                currentToken As SyntaxToken,
                optionSet As OptionSet,
                ByRef nextOperation As NextGetAdjustNewLinesOperation) As AdjustNewLinesOperation

            ' if it doesn't have elastic trivia, pass it through
            If Not CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken) Then
                Return nextOperation.Invoke()
            End If

            ' if it has one, check whether there is a forced one
            Dim operation = nextOperation.Invoke()

            If operation IsNot Nothing AndAlso operation.Option = AdjustNewLinesOption.ForceLines Then
                Return operation
            End If

            If currentToken.Kind = SyntaxKind.DotToken AndAlso
               currentToken.Parent.Kind = SyntaxKind.NamedFieldInitializer Then

                Return New AdjustNewLinesOperation(line:=1,
                    [option]:=AdjustNewLinesOption.ForceLines)
            End If

            If previousToken.Kind = SyntaxKind.OpenBraceToken AndAlso
               previousToken.Parent.Kind = SyntaxKind.CollectionInitializer AndAlso
               previousToken.Parent.Parent.Kind = SyntaxKind.ObjectCollectionInitializer Then

                Return New AdjustNewLinesOperation(line:=1,
                    [option]:=AdjustNewLinesOption.ForceLines)
            End If

            If previousToken.Kind = SyntaxKind.CommaToken AndAlso
               previousToken.Parent.Kind = SyntaxKind.CollectionInitializer AndAlso
               previousToken.Parent.Parent.Kind = SyntaxKind.ObjectCollectionInitializer Then

                Return New AdjustNewLinesOperation(line:=1,
                    [option]:=AdjustNewLinesOption.ForceLines)
            End If

            If currentToken.Kind = SyntaxKind.OpenBraceToken AndAlso
               currentToken.Parent.Kind = SyntaxKind.CollectionInitializer AndAlso
               currentToken.Parent.Parent.Kind = SyntaxKind.CollectionInitializer Then
                Return New AdjustNewLinesOperation(line:=1,
                    [option]:=AdjustNewLinesOption.ForceLines)
            End If

            If currentToken.Kind = SyntaxKind.CloseBraceToken Then
                If currentToken.Parent.Kind = SyntaxKind.ObjectMemberInitializer Then

                    Return New AdjustNewLinesOperation(line:=1,
                        [option]:=AdjustNewLinesOption.ForceLines)
                End If

                If currentToken.Parent.Kind = SyntaxKind.CollectionInitializer AndAlso
                   currentToken.Parent.Parent.Kind = SyntaxKind.ObjectCollectionInitializer Then
                    Return New AdjustNewLinesOperation(line:=1,
                        [option]:=AdjustNewLinesOption.ForceLines)
                End If
            End If

            ' put attributes in its own line if it is top level attribute
            Dim attributeNode = TryCast(previousToken.Parent, AttributeListSyntax)
            If attributeNode IsNot Nothing AndAlso TypeOf attributeNode.Parent Is StatementSyntax AndAlso
               attributeNode.GreaterThanToken = previousToken AndAlso currentToken.Kind <> SyntaxKind.LessThanToken Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines)
            End If

            If Not previousToken.IsLastTokenOfStatement() Then
                Return operation
            End If

            ' The previous token may end a statement, but it could be in a lambda inside parens.
            If currentToken.Kind = SyntaxKind.CloseParenToken AndAlso
               TypeOf currentToken.Parent Is ParenthesizedExpressionSyntax Then

                Return operation
            End If

            If AfterLastInheritsOrImplements(previousToken, currentToken) Then
                If Not TypeOf currentToken.Parent Is EndBlockStatementSyntax Then
                    Return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines)
                End If
            End If

            If AfterLastImportStatement(previousToken, currentToken) Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines)
            End If

            Dim lines = LineBreaksAfter(previousToken, currentToken)
            If Not lines.HasValue Then
                If TypeOf previousToken.Parent Is XmlNodeSyntax Then
                    ' make sure next statement starts on its own line if previous statement ends with xml literals
                    Return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines)
                End If

                Return CreateAdjustNewLinesOperation(Math.Max(If(operation Is Nothing, 1, operation.Line), 0), AdjustNewLinesOption.PreserveLines)
            End If

            If lines = 0 Then
                Return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            Return CreateAdjustNewLinesOperation(lines.Value, AdjustNewLinesOption.ForceLines)
        End Function

        Private Function AfterLastImportStatement(token As SyntaxToken, nextToken As SyntaxToken) As Boolean
            ' in between two imports
            If nextToken.Kind = SyntaxKind.ImportsKeyword Then
                Return False
            End If

            ' current one is not import statement
            If Not TypeOf token.Parent Is NameSyntax Then
                Return False
            End If

            Dim [imports] = token.GetAncestor(Of ImportsStatementSyntax)()
            If [imports] Is Nothing Then
                Return False
            End If

            Return True
        End Function

        Private Function AfterLastInheritsOrImplements(token As SyntaxToken, nextToken As SyntaxToken) As Boolean
            Dim inheritsOrImplements = token.GetAncestor(Of InheritsOrImplementsStatementSyntax)()
            Dim nextInheritsOrImplements = nextToken.GetAncestor(Of InheritsOrImplementsStatementSyntax)()

            Return inheritsOrImplements IsNot Nothing AndAlso nextInheritsOrImplements Is Nothing
        End Function

        Private Function IsBeginStatement(Of TStatement As StatementSyntax, TBlock As StatementSyntax)(node As StatementSyntax) As Boolean
            Return TryCast(node, TStatement) IsNot Nothing AndAlso TryCast(node.Parent, TBlock) IsNot Nothing
        End Function

        Private Function IsEndBlockStatement(node As StatementSyntax) As Boolean
            Return TryCast(node, EndBlockStatementSyntax) IsNot Nothing OrElse
                TryCast(node, LoopStatementSyntax) IsNot Nothing OrElse
                TryCast(node, NextStatementSyntax) IsNot Nothing
        End Function

        Private Function LineBreaksAfter(previousToken As SyntaxToken, currentToken As SyntaxToken) As Integer?
            If currentToken.Kind = SyntaxKind.None OrElse
               previousToken.Kind = SyntaxKind.None Then
                Return 0
            End If

            Dim previousStatement = previousToken.GetAncestor(Of StatementSyntax)()
            Dim currentStatement = currentToken.GetAncestor(Of StatementSyntax)()

            If previousStatement Is Nothing OrElse currentStatement Is Nothing Then
                Return Nothing
            End If

            If TopLevelStatement(previousStatement) AndAlso Not TopLevelStatement(currentStatement) Then
                Return GetActualLines(previousToken, currentToken, 1)
            End If

            ' Early out of accessors, we don't force more lines between them.
            If previousStatement.Kind = SyntaxKind.EndSetStatement OrElse
               previousStatement.Kind = SyntaxKind.EndGetStatement OrElse
               previousStatement.Kind = SyntaxKind.EndAddHandlerStatement OrElse
               previousStatement.Kind = SyntaxKind.EndRemoveHandlerStatement OrElse
               previousStatement.Kind = SyntaxKind.EndRaiseEventStatement Then
                Return Nothing
            End If

            ' Blank line after an end block, unless it's followed by another end or an else
            If IsEndBlockStatement(previousStatement) Then
                If IsEndBlockStatement(currentStatement) OrElse
                   currentStatement.Kind = SyntaxKind.ElseIfStatement OrElse
                   currentStatement.Kind = SyntaxKind.ElseStatement Then
                    Return GetActualLines(previousToken, currentToken, 1)
                Else
                    Return GetActualLines(previousToken, currentToken, 2, 1)
                End If
            End If

            ' Blank line _before_ a block, unless it's the first thing in a type.
            If IsBeginStatement(Of MethodStatementSyntax, MethodBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of SubNewStatementSyntax, ConstructorBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of OperatorStatementSyntax, OperatorBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of PropertyStatementSyntax, PropertyBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of EventStatementSyntax, EventBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of TypeStatementSyntax, TypeBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of EnumStatementSyntax, EnumBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of NamespaceStatementSyntax, NamespaceBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of DoStatementSyntax, DoLoopBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of ForStatementSyntax, ForOrForEachBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of ForEachStatementSyntax, ForOrForEachBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of IfStatementSyntax, MultiLineIfBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of SelectStatementSyntax, SelectBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of SyncLockStatementSyntax, SyncLockBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of TryStatementSyntax, TryBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of UsingStatementSyntax, UsingBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of WhileStatementSyntax, WhileBlockSyntax)(currentStatement) OrElse
               IsBeginStatement(Of WithStatementSyntax, WithBlockSyntax)(currentStatement) Then

                If TypeOf previousStatement Is NamespaceStatementSyntax OrElse
                   TypeOf previousStatement Is TypeStatementSyntax Then
                    Return GetActualLines(previousToken, currentToken, 1)
                Else
                    Return GetActualLines(previousToken, currentToken, 2, 1)
                End If
            End If

            Return Nothing
        End Function

        Private Function GetActualLines(token1 As SyntaxToken, token2 As SyntaxToken, lines As Integer, Optional leadingBlankLines As Integer = 0) As Integer
            If leadingBlankLines = 0 Then
                Return Math.Max(lines, 0)
            End If

            ' see whether first non whitespace trivia after previous member is comment or not
            Dim list = token1.TrailingTrivia.Concat(token2.LeadingTrivia)

            Dim firstNonWhitespaceTrivia = list.FirstOrDefault(Function(t) Not t.IsWhitespaceOrEndOfLine())
            If firstNonWhitespaceTrivia.IsKind(SyntaxKind.CommentTrivia, SyntaxKind.DocumentationCommentTrivia) Then
                Dim totalLines = GetNumberOfLines(list)
                Dim blankLines = GetNumberOfLines(list.TakeWhile(Function(t) t <> firstNonWhitespaceTrivia))

                If totalLines < lines Then
                    Dim afterCommentWithBlank = (totalLines - blankLines) + leadingBlankLines
                    Return Math.Max(If(lines > afterCommentWithBlank, lines, afterCommentWithBlank), 0)
                End If

                If blankLines < leadingBlankLines Then
                    Return Math.Max(totalLines - blankLines + leadingBlankLines, 0)
                End If

                Return Math.Max(totalLines, 0)
            End If

            Return Math.Max(lines, 0)
        End Function

        Private Function GetNumberOfLines(list As IEnumerable(Of SyntaxTrivia)) As Integer
            Return list.Sum(Function(t) t.ToFullString().Replace(vbCrLf, vbCr).OfType(Of Char).Count(Function(c) SyntaxFacts.IsNewLine(c)))
        End Function

        Private Function TopLevelStatement(statement As StatementSyntax) As Boolean
            Return TypeOf statement Is MethodStatementSyntax OrElse
                   TypeOf statement Is SubNewStatementSyntax OrElse
                   TypeOf statement Is LambdaHeaderSyntax OrElse
                   TypeOf statement Is OperatorStatementSyntax OrElse
                   TypeOf statement Is PropertyStatementSyntax OrElse
                   TypeOf statement Is EventStatementSyntax OrElse
                   TypeOf statement Is TypeStatementSyntax OrElse
                   TypeOf statement Is EnumStatementSyntax OrElse
                   TypeOf statement Is NamespaceStatementSyntax
        End Function
    End Class
End Namespace
