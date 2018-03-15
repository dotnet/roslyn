' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.InvertIf
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertIf), [Shared]>
    Friend Class InvertIfCodeRefactoringProvider
        Inherits AbstractInvertIfCodeRefactoringProvider

        Protected Overrides Function GetIfStatement(textSpan As TextSpan, token As SyntaxToken, cancellationToken As CancellationToken) As SyntaxNode
            ' We need to find a relevant if-else statement of type SingleLineIfStatement or
            ' MultiLineIfBlock to act on its IfPart and ElsePart.
            Dim relevantIfBlockOrIfStatement As ExecutableStatementSyntax = Nothing

            ' We also need to find the relevant if statement's span in that if-(elseif)-else
            ' statement to indicate where the refactoring should happen. The relevant if statement
            ' could be the top statement in an if block (if it consists of an If and an Else) or it
            ' could be the last ElseIf statement in a block before an Else statement.
            Dim relevantSpan As TextSpan = Nothing

            Dim singleLineIf = token.GetAncestor(Of SingleLineIfStatementSyntax)()
            Dim multiLineIf = token.GetAncestor(Of MultiLineIfBlockSyntax)()

            ' Tweak
            If singleLineIf IsNot Nothing AndAlso
               singleLineIf.ElseClause IsNot Nothing Then

                relevantIfBlockOrIfStatement = singleLineIf
                relevantSpan = singleLineIf.Span

            ElseIf multiLineIf IsNot Nothing AndAlso
                   multiLineIf.ElseBlock IsNot Nothing Then

                relevantIfBlockOrIfStatement = multiLineIf

                If multiLineIf.ElseIfBlocks.IsEmpty Then
                    relevantSpan = multiLineIf.IfStatement.IfKeyword.Span
                Else
                    Dim elseIfBlock = token.GetAncestor(Of ElseIfBlockSyntax)()

                    ' The MultiLineIfBlockSyntax has ElseIfBlocks and now we want to find out if the
                    ' user has placed the cursor on the last ElseIfPart or a node contained in the
                    ' last ElseIfPart to decide whether or not we should provide the code action
                    If elseIfBlock IsNot Nothing AndAlso
                       elseIfBlock Is multiLineIf.ElseIfBlocks.Last Then

                        relevantSpan = elseIfBlock.ElseIfStatement.ElseIfKeyword.Span
                    Else
                        Return Nothing
                    End If
                End If
            Else
                Return Nothing
            End If

            If Not relevantSpan.IntersectsWith(textSpan.Start) Then
                Return Nothing
            End If

            If token.SyntaxTree.OverlapsHiddenPosition(relevantSpan, cancellationToken) Then
                Return Nothing
            End If

            Return relevantIfBlockOrIfStatement
        End Function

        Protected Overrides Function GetRootWithInvertIfStatement(workspace As Workspace, model As SemanticModel, ifStatement As SyntaxNode, cancellationToken As CancellationToken) As SyntaxNode
            Dim result = UpdateSemanticModel(model, model.SyntaxTree.GetRoot().ReplaceNode(ifStatement, ifStatement.WithAdditionalAnnotations(s_ifNodeAnnotation)), cancellationToken)

            Dim ifNode = FindIfNode(result.Root)

            ' Complexify the top-most statement parenting this if-statement if necessary
            Dim topMostExpression = ifNode.Ancestors().OfType(Of ExpressionSyntax).LastOrDefault()
            If topMostExpression IsNot Nothing Then
                Dim topMostStatement = topMostExpression.Ancestors().OfType(Of StatementSyntax).FirstOrDefault()
                If topMostStatement IsNot Nothing Then
                    Dim explicitTopMostStatement = Simplifier.Expand(topMostStatement, result.Model, workspace, cancellationToken:=cancellationToken)
                    result = UpdateSemanticModel(result.Model, result.Root.ReplaceNode(topMostStatement, explicitTopMostStatement), cancellationToken)
                    ifNode = FindIfNode(result.Root)
                End If
            End If

            If (TypeOf ifNode Is SingleLineIfStatementSyntax) Then
                model = InvertSingleLineIfStatement(workspace, DirectCast(ifNode, SingleLineIfStatementSyntax), result.Model, cancellationToken)
            Else
                model = InvertMultiLineIfBlock(DirectCast(ifNode, MultiLineIfBlockSyntax), result.Model, cancellationToken)
            End If

            ' Complexify the inverted if node.
            result = (model, model.SyntaxTree.GetRoot())
            Dim invertedIfNode = FindIfNode(result.Root)

            Dim explicitInvertedIfNode = Simplifier.Expand(invertedIfNode, result.Model, workspace, cancellationToken:=cancellationToken)
            result = UpdateSemanticModel(result.Model, result.Root.ReplaceNode(invertedIfNode, explicitInvertedIfNode), cancellationToken)

            Return result.Root
        End Function

        Private Function UpdateSemanticModel(model As SemanticModel, root As SyntaxNode, cancellationToken As CancellationToken) As (Model As SemanticModel, Root As SyntaxNode)
            Dim newModel = model.Compilation.ReplaceSyntaxTree(model.SyntaxTree, root.SyntaxTree).GetSemanticModel(root.SyntaxTree)
            Return (newModel, newModel.SyntaxTree.GetRoot(cancellationToken))
        End Function

        Private Shared ReadOnly s_comparisonInversesMap As Dictionary(Of SyntaxKind, Tuple(Of SyntaxKind, SyntaxKind)) =
            New Dictionary(Of SyntaxKind, Tuple(Of SyntaxKind, SyntaxKind))(SyntaxFacts.EqualityComparer) From
            {
                {SyntaxKind.EqualsExpression, Tuple.Create(SyntaxKind.NotEqualsExpression, SyntaxKind.LessThanGreaterThanToken)},
                {SyntaxKind.NotEqualsExpression, Tuple.Create(SyntaxKind.EqualsExpression, SyntaxKind.EqualsToken)},
                {SyntaxKind.LessThanExpression, Tuple.Create(SyntaxKind.GreaterThanOrEqualExpression, SyntaxKind.GreaterThanEqualsToken)},
                {SyntaxKind.LessThanOrEqualExpression, Tuple.Create(SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanToken)},
                {SyntaxKind.GreaterThanExpression, Tuple.Create(SyntaxKind.LessThanOrEqualExpression, SyntaxKind.LessThanEqualsToken)},
                {SyntaxKind.GreaterThanOrEqualExpression, Tuple.Create(SyntaxKind.LessThanExpression, SyntaxKind.LessThanToken)},
                {SyntaxKind.IsExpression, Tuple.Create(SyntaxKind.IsNotExpression, SyntaxKind.IsNotKeyword)},
                {SyntaxKind.IsNotExpression, Tuple.Create(SyntaxKind.IsExpression, SyntaxKind.IsKeyword)}
            }

        Private Shared ReadOnly s_logicalInversesMap As Dictionary(Of SyntaxKind, Tuple(Of SyntaxKind, SyntaxKind)) =
            New Dictionary(Of SyntaxKind, Tuple(Of SyntaxKind, SyntaxKind))(SyntaxFacts.EqualityComparer) From
            {
                {SyntaxKind.OrExpression, Tuple.Create(SyntaxKind.AndExpression, SyntaxKind.AndKeyword)},
                {SyntaxKind.AndExpression, Tuple.Create(SyntaxKind.OrExpression, SyntaxKind.OrKeyword)},
                {SyntaxKind.OrElseExpression, Tuple.Create(SyntaxKind.AndAlsoExpression, SyntaxKind.AndAlsoKeyword)},
                {SyntaxKind.AndAlsoExpression, Tuple.Create(SyntaxKind.OrElseExpression, SyntaxKind.OrElseKeyword)}
            }

        Private Shared ReadOnly s_ifNodeAnnotation As New SyntaxAnnotation

        Protected Shared Async Function FindIfNodeAsync(document As Document, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Return FindIfNode(root)
        End Function

        Private Shared Function FindIfNode(root As SyntaxNode) As SyntaxNode
            Return root _
                       .GetAnnotatedNodesAndTokens(s_ifNodeAnnotation) _
                       .Single() _
                       .AsNode()
        End Function

        Private Function InvertSingleLineIfStatement(workspace As Workspace, originalIfNode As SingleLineIfStatementSyntax, model As SemanticModel, cancellationToken As CancellationToken) As SemanticModel
            Dim root = model.SyntaxTree.GetRoot()

            Dim invertedIfNode = GetInvertedIfNode(originalIfNode, model, cancellationToken) _
                .WithAdditionalAnnotations(Formatter.Annotation)

            Dim result = UpdateSemanticModel(model, root.ReplaceNode(originalIfNode, invertedIfNode), cancellationToken)

            ' Complexify the next statement if there is one.
            invertedIfNode = DirectCast(FindIfNode(result.Root), SingleLineIfStatementSyntax)

            Dim currentStatement As StatementSyntax = invertedIfNode
            If currentStatement.HasAncestor(Of ExpressionSyntax)() Then
                currentStatement = currentStatement _
                    .Ancestors() _
                    .OfType(Of ExpressionSyntax) _
                    .Last() _
                    .FirstAncestorOrSelf(Of StatementSyntax)()
            End If

            Dim nextStatement = currentStatement.GetNextStatement()
            If nextStatement IsNot Nothing Then
                Dim explicitNextStatement = Simplifier.Expand(nextStatement, result.Model, workspace, cancellationToken:=cancellationToken)
                result = UpdateSemanticModel(result.Model, result.Root.ReplaceNode(nextStatement, explicitNextStatement), cancellationToken)
            End If

            Return result.Model
        End Function

        Private Function GetInvertedIfNode(
            ifNode As SingleLineIfStatementSyntax,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken) As SingleLineIfStatementSyntax

            Dim elseClause = ifNode.ElseClause

            ' If we're moving a single line if from the else body to the if body,
            ' and it is the last statement in the body, we have to introduce an extra
            ' StatementTerminator Colon and Else token.

            Dim newIfStatements = elseClause.Statements

            If newIfStatements.Count > 0 Then
                newIfStatements = newIfStatements.Replace(newIfStatements.Last,
                                                          newIfStatements.Last.WithTrailingTrivia(elseClause.ElseKeyword.GetPreviousToken().TrailingTrivia))
            End If

            If elseClause.Statements.Count > 0 AndAlso
               elseClause.Statements.Last().Kind = SyntaxKind.SingleLineIfStatement Then

                Dim singleLineIf = DirectCast(elseClause.Statements.Last, SingleLineIfStatementSyntax)

                ' Create an Extra 'Else'
                If singleLineIf.ElseClause Is Nothing Then

                    ' Replace the last EOL of the IfPart with a :
                    Dim trailing = singleLineIf.GetTrailingTrivia()
                    If trailing.Any(SyntaxKind.EndOfLineTrivia) Then
                        Dim eol = trailing.Last(Function(t) t.Kind = SyntaxKind.EndOfLineTrivia)
                        trailing = trailing.Select(Function(t) If(t = eol, SyntaxFactory.ColonTrivia(SyntaxFacts.GetText(SyntaxKind.ColonTrivia)), t)).ToSyntaxTriviaList()
                    End If

                    Dim withElsePart = singleLineIf.WithTrailingTrivia(trailing).WithElseClause(
                        SyntaxFactory.SingleLineElseClause(SyntaxFactory.List(Of StatementSyntax)()))

                    ' Put the if statement with the else into the statement list
                    newIfStatements = elseClause.Statements.Replace(elseClause.Statements.Last, withElsePart)
                End If
            End If

            Return ifNode.WithCondition(Negate(ifNode.Condition, semanticModel, cancellationToken)) _
                         .WithStatements(newIfStatements) _
                         .WithElseClause(elseClause.WithStatements(ifNode.Statements).WithTrailingTrivia(elseClause.GetTrailingTrivia()))
        End Function

#If False Then
        ' If we have a : following the outermost SingleLineIf, we'll want to remove it and use a statementterminator token instead.
        ' This ensures that the following statement will stand alone instead of becoming part of the if, as discussed in Bug 14259.
        Private Function UpdateStatementList(invertedIfNode As SingleLineIfStatementSyntax, originalIfNode As SingleLineIfStatementSyntax, cancellationToken As CancellationToken) As SyntaxList(Of StatementSyntax)
            Dim parentMultiLine = originalIfNode.GetContainingMultiLineExecutableBlocks().FirstOrDefault
            Dim statements = parentMultiLine.GetExecutableBlockStatements()

            Dim index = statements.IndexOf(originalIfNode)

            If index < 0 Then
                Return Nothing
            End If

            If Not invertedIfNode.GetTrailingTrivia().Any(Function(t) t.Kind = SyntaxKind.ColonTrivia) Then
                Return Nothing
            End If

            ' swap colon trivia to EOL
            Return SyntaxFactory.List(
                statements.Replace(
                    originalIfNode,
                    invertedIfNode.WithTrailingTrivia(
                        invertedIfNode.GetTrailingTrivia().Select(
                            Function(t) If(t.Kind = SyntaxKind.ColonTrivia, SyntaxFactory.CarriageReturnLineFeed, t)))))
        End Function
#End If

        Private Function InvertMultiLineIfBlock(originalIfNode As MultiLineIfBlockSyntax, model As SemanticModel, cancellationToken As CancellationToken) As SemanticModel
            Dim invertedIfNode = GetInvertedIfNode(originalIfNode, model, cancellationToken) _
                .WithAdditionalAnnotations(Formatter.Annotation)

            Dim result = UpdateSemanticModel(model, model.SyntaxTree.GetRoot().ReplaceNode(originalIfNode, invertedIfNode), cancellationToken)
            Return result.Model
        End Function

        Private Function GetInvertedIfNode(
            ifNode As MultiLineIfBlockSyntax,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken) As MultiLineIfBlockSyntax

            Dim ifPart = ifNode
            Dim elseBlock = ifNode.ElseBlock

            If ifNode.ElseIfBlocks.IsEmpty Then
                ' Since this block has no ElseIf parts, we can simply negate the condition
                ' and swap the statements in the IfPart and the ElsePart
                Dim ifStatement = ifNode.IfStatement

                Return ifNode _
                    .WithIfStatement(
                        ifStatement.WithCondition(
                            Negate(ifStatement.Condition, semanticModel, cancellationToken)
                        )
                     ) _
                    .WithStatements(elseBlock.Statements) _
                    .WithElseBlock(
                        elseBlock.WithStatements(ifPart.Statements) _
                                 .WithLeadingTrivia(ifNode.EndIfStatement.GetLeadingTrivia())
                     ) _
                    .WithEndIfStatement(ifNode.EndIfStatement.WithLeadingTrivia(elseBlock.GetLeadingTrivia()))
            Else
                ' Since this block has one or more ElseIf parts, we are acting on the last
                ' ElseIf, which we have to find in the block's list of ElseIf parts to replace,
                ' and the ElsePart
                Dim oldElseIfBlock = ifNode.ElseIfBlocks.Last
                Dim oldElseIfStatement = oldElseIfBlock.ElseIfStatement

                Dim newElseIfStatement = oldElseIfStatement.WithCondition(Negate(oldElseIfStatement.Condition, semanticModel, cancellationToken)) _
                                                           .WithAdditionalAnnotations(Formatter.Annotation)

                Dim newElseIfBlock = oldElseIfBlock.WithElseIfStatement(newElseIfStatement) _
                                                   .WithStatements(elseBlock.Statements) _
                                                   .WithAdditionalAnnotations(Formatter.Annotation)

                Return ifNode.ReplaceNode(oldElseIfBlock, newElseIfBlock) _
                             .WithElseBlock(ifNode.ElseBlock.WithStatements(oldElseIfBlock.Statements) _
                                                            .WithLeadingTrivia(ifNode.EndIfStatement.GetLeadingTrivia())) _
                             .WithEndIfStatement(ifNode.EndIfStatement.WithLeadingTrivia(ifNode.ElseBlock.GetLeadingTrivia()))
            End If
        End Function

        Private Function TryNegateBinaryComparisonExpression(
            expression As ExpressionSyntax,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken,
            ByRef result As ExpressionSyntax) As Boolean

            Dim inverses As Tuple(Of SyntaxKind, SyntaxKind) = Nothing
            If s_comparisonInversesMap.TryGetValue(expression.Kind, inverses) Then
                Dim binaryExpression = DirectCast(expression, BinaryExpressionSyntax)
                Dim expressionType = inverses.Item1
                Dim operatorType = inverses.Item2

                ' Special case negating Length > 0 to Length = 0 and 0 < Length to 0 == Length
                ' for arrays and strings. We can do this because we know that Length cannot be
                ' less than 0.
                Dim operation = semanticModel.GetOperation(binaryExpression)
                If (IsSpecialCaseBinaryExpression(TryCast(operation, IBinaryOperation), cancellationToken)) Then
                    expressionType = SyntaxKind.EqualsExpression
                    operatorType = SyntaxKind.EqualsToken
                End If

                result = SyntaxFactory.BinaryExpression(
                    expressionType,
                    binaryExpression.Left.Parenthesize(),
                    SyntaxFactory.Token(
                        binaryExpression.OperatorToken.LeadingTrivia,
                        operatorType,
                        binaryExpression.OperatorToken.TrailingTrivia),
                    binaryExpression.Right.Parenthesize())

                result = result _
                .WithLeadingTrivia(binaryExpression.GetLeadingTrivia()) _
                .WithTrailingTrivia(binaryExpression.GetTrailingTrivia())

                Return True
            End If

            Return False
        End Function

        Private Function TryNegateBinaryLogicalExpression(
            expression As ExpressionSyntax,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken,
            ByRef result As ExpressionSyntax) As Boolean

            Dim inverses As Tuple(Of SyntaxKind, SyntaxKind) = Nothing
            If s_logicalInversesMap.TryGetValue(expression.Kind, inverses) Then
                Dim binaryExpression = DirectCast(expression, BinaryExpressionSyntax)

                ' NOTE: result must be parenthesized because And & AndAlso have higher precedence than Or & OrElse
                result = SyntaxFactory.BinaryExpression(
                    inverses.Item1,
                    Negate(binaryExpression.Left, semanticModel, cancellationToken),
                    SyntaxFactory.Token(
                        binaryExpression.OperatorToken.LeadingTrivia,
                        inverses.Item2,
                        binaryExpression.OperatorToken.TrailingTrivia),
                    Negate(binaryExpression.Right, semanticModel, cancellationToken))

                result = result _
                    .Parenthesize() _
                    .WithLeadingTrivia(binaryExpression.GetLeadingTrivia()) _
                    .WithTrailingTrivia(binaryExpression.GetTrailingTrivia())

                Return True
            End If

            Return False
        End Function

        Protected Function Negate(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ExpressionSyntax
            Dim result As ExpressionSyntax = Nothing

            If TryNegateBinaryComparisonExpression(expression, semanticModel, cancellationToken, result) OrElse
               TryNegateBinaryLogicalExpression(expression, semanticModel, cancellationToken, result) Then
                Return result.WithAdditionalAnnotations(Formatter.Annotation)
            End If

            Select Case expression.Kind
                Case SyntaxKind.ParenthesizedExpression
                    Dim parenthesizedExpression = DirectCast(expression, ParenthesizedExpressionSyntax)
                    Return parenthesizedExpression _
                        .WithExpression(Negate(parenthesizedExpression.Expression, semanticModel, cancellationToken)) _
                        .WithAdditionalAnnotations({Simplifier.Annotation})

                Case SyntaxKind.NotExpression
                    Dim notExpression = DirectCast(expression, UnaryExpressionSyntax)

                    Dim notToken = notExpression.OperatorToken
                    Dim nextToken = notExpression.Operand.GetFirstToken(includeZeroWidth:=True, includeSkipped:=True, includeDirectives:=True, includeDocumentationComments:=True)
                    Dim updatedNextToken = nextToken.WithLeadingTrivia(notToken.LeadingTrivia)

                    Return notExpression.Operand.ReplaceToken(nextToken, updatedNextToken).WithAdditionalAnnotations(Simplifier.Annotation)

                Case SyntaxKind.TrueLiteralExpression
                    Return SyntaxFactory.FalseLiteralExpression(
                        SyntaxFactory.Token(expression.GetLeadingTrivia(),
                                     SyntaxKind.FalseKeyword,
                                     expression.GetTrailingTrivia()))

                Case SyntaxKind.FalseLiteralExpression
                    Return SyntaxFactory.TrueLiteralExpression(
                        SyntaxFactory.Token(expression.GetLeadingTrivia(),
                                     SyntaxKind.TrueKeyword,
                                     expression.GetTrailingTrivia()))
            End Select

            ' Anything else can be negated by adding Not in front of the expression
            result = SyntaxFactory.UnaryExpression(
                SyntaxKind.NotExpression,
                SyntaxFactory.Token(SyntaxKind.NotKeyword),
                expression.Parenthesize())

            Return result.WithAdditionalAnnotations(Formatter.Annotation)
        End Function
    End Class
End Namespace
