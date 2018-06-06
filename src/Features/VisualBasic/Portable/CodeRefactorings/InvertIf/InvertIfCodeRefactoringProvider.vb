' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.InvertIf
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertIf), [Shared]>
    Friend Class VisualBasicInvertIfCodeRefactoringProvider
        Inherits AbstractInvertIfCodeRefactoringProvider

        Private Shared ReadOnly s_ifNodeAnnotation As New SyntaxAnnotation

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Protected Overrides Function GetIfStatement(textSpan As TextSpan, token As SyntaxToken, cancellationToken As CancellationToken) As SyntaxNode

            Dim relevantIfBlockOrIfStatement As ExecutableStatementSyntax = Nothing
            Dim relevantSpan As TextSpan = Nothing

            Dim singleLineIf = token.GetAncestor(Of SingleLineIfStatementSyntax)()
            Dim multiLineIf = token.GetAncestor(Of MultiLineIfBlockSyntax)()

            If singleLineIf IsNot Nothing AndAlso
               singleLineIf.ElseClause IsNot Nothing Then

                relevantIfBlockOrIfStatement = singleLineIf
                relevantSpan = TextSpan.FromBounds(singleLineIf.IfKeyword.Span.Start, singleLineIf.Condition.Span.End)

            ElseIf multiLineIf IsNot Nothing AndAlso
                   multiLineIf.ElseBlock IsNot Nothing AndAlso
                   multiLineIf.ElseIfBlocks.IsEmpty Then

                relevantIfBlockOrIfStatement = multiLineIf
                relevantSpan = TextSpan.FromBounds(multiLineIf.IfStatement.IfKeyword.Span.Start, multiLineIf.IfStatement.Condition.Span.End)
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

        Protected Overrides Function GetRootWithInvertIfStatement(document As Document, model As SemanticModel, ifStatement As SyntaxNode, cancellationToken As CancellationToken) As SyntaxNode

            Dim generator = SyntaxGenerator.GetGenerator(document)
            Dim syntaxFacts = GetSyntaxFactsService()

            Dim result = UpdateSemanticModel(model, model.SyntaxTree.GetRoot().ReplaceNode(ifStatement, ifStatement.WithAdditionalAnnotations(s_ifNodeAnnotation)), cancellationToken)

            Dim ifNode = result.Root.GetAnnotatedNodesAndTokens(s_ifNodeAnnotation).Single().AsNode()

            'In order to add parentheses for SingleLineIfStatements with commas, such as
            'Case Sub() [||]If True Then Dim x Else Return, Nothing
            'complexify the top-most statement parenting this if-statement if necessary
            Dim topMostExpression = ifNode.Ancestors().OfType(Of ExpressionSyntax).LastOrDefault()
            If topMostExpression IsNot Nothing Then
                Dim topMostStatement = topMostExpression.Ancestors().OfType(Of StatementSyntax).FirstOrDefault()
                If topMostStatement IsNot Nothing Then
                    Dim explicitTopMostStatement = Simplifier.Expand(topMostStatement, result.Model, document.Project.Solution.Workspace, cancellationToken:=cancellationToken)
                    result = UpdateSemanticModel(result.Model, result.Root.ReplaceNode(topMostStatement, explicitTopMostStatement), cancellationToken)
                    ifNode = result.Root.GetAnnotatedNodesAndTokens(s_ifNodeAnnotation).Single().AsNode()
                End If
            End If

            If (TypeOf ifNode Is SingleLineIfStatementSyntax) Then
                model = InvertSingleLineIfStatement(document, DirectCast(ifNode, SingleLineIfStatementSyntax), generator, syntaxFacts, result.Model, cancellationToken)
            Else
                model = InvertMultiLineIfBlock(DirectCast(ifNode, MultiLineIfBlockSyntax), document, generator, syntaxFacts, result.Model, cancellationToken)
            End If

            ' Complexify the inverted if node.
            result = (model, model.SyntaxTree.GetRoot())
            Dim invertedIfNode = result.Root.GetAnnotatedNodesAndTokens(s_ifNodeAnnotation).Single().AsNode()

            Dim explicitInvertedIfNode = Simplifier.Expand(invertedIfNode, result.Model, document.Project.Solution.Workspace, cancellationToken:=cancellationToken)
            result = UpdateSemanticModel(result.Model, result.Root.ReplaceNode(invertedIfNode, explicitInvertedIfNode), cancellationToken)

            Return result.Root
        End Function

        Private Function UpdateSemanticModel(model As SemanticModel, root As SyntaxNode, cancellationToken As CancellationToken) As (Model As SemanticModel, Root As SyntaxNode)
            Dim newModel = model.Compilation.ReplaceSyntaxTree(model.SyntaxTree, root.SyntaxTree).GetSemanticModel(root.SyntaxTree)
            Return (newModel, newModel.SyntaxTree.GetRoot(cancellationToken))
        End Function

        Private Function InvertSingleLineIfStatement(
                document As Document,
                originalIfNode As SingleLineIfStatementSyntax,
                generator As SyntaxGenerator,
                syntaxFacts As ISyntaxFactsService,
                model As SemanticModel,
                cancellationToken As CancellationToken) As SemanticModel
            Dim root = model.SyntaxTree.GetRoot()
            Dim invertedIfNode = GetInvertedIfNode(originalIfNode, document, generator, syntaxFacts, model, cancellationToken)
            Dim result = UpdateSemanticModel(model, root.ReplaceNode(originalIfNode, invertedIfNode), cancellationToken)

            ' Complexify the next statement if there is one.
            invertedIfNode = DirectCast(result.Root.GetAnnotatedNodesAndTokens(s_ifNodeAnnotation).Single().AsNode(), SingleLineIfStatementSyntax)

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
                Dim explicitNextStatement = Simplifier.Expand(nextStatement, result.Model, document.Project.Solution.Workspace, cancellationToken:=cancellationToken)
                result = UpdateSemanticModel(result.Model, result.Root.ReplaceNode(nextStatement, explicitNextStatement), cancellationToken)
            End If

            Return result.Model
        End Function

        Private Function GetInvertedIfNode(
            ifNode As SingleLineIfStatementSyntax,
            document As Document,
            generator As SyntaxGenerator,
            syntaxFacts As ISyntaxFactsService,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken) As SingleLineIfStatementSyntax

            Dim elseClause = ifNode.ElseClause

            ' If we're moving a single line if from the else body to the if body,
            ' and it is the last statement in the body, we have to introduce an extra
            ' StatementTerminator Colon and Else token.
            Dim newIfStatements = elseClause.Statements

            If newIfStatements.Count > 0 Then
                newIfStatements = newIfStatements.Replace(
                    newIfStatements.Last,
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
                        trailing = trailing.Select(Function(t) If(t = eol, SyntaxFactory.ColonTrivia(syntaxFacts.GetText(SyntaxKind.ColonTrivia)), t)).ToSyntaxTriviaList()
                    End If

                    Dim withElsePart = singleLineIf.WithTrailingTrivia(trailing).WithElseClause(
                        SyntaxFactory.SingleLineElseClause(SyntaxFactory.List(Of StatementSyntax)()))

                    ' Put the if statement with the else into the statement list
                    newIfStatements = elseClause.Statements.Replace(elseClause.Statements.Last, withElsePart)
                End If
            End If

            Return ifNode.WithCondition(DirectCast(Negate(ifNode.Condition, generator, syntaxFacts, semanticModel, cancellationToken), ExpressionSyntax)) _
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
                            Function(t) If(t.Kind = SyntaxKind.ColonTrivia, SyntaxFactory.ElasticCarriageReturnLineFeed, t)))))
        End Function
#End If

        Private Function InvertMultiLineIfBlock(originalIfNode As MultiLineIfBlockSyntax, document As Document, generator As SyntaxGenerator, syntaxFacts As ISyntaxFactsService, model As SemanticModel, cancellationToken As CancellationToken) As SemanticModel
            Dim invertedIfNode = GetInvertedIfNode(originalIfNode, document, generator, syntaxFacts, model, cancellationToken)

            Dim result = UpdateSemanticModel(model, model.SyntaxTree.GetRoot().ReplaceNode(originalIfNode, invertedIfNode), cancellationToken)
            Return result.Model
        End Function

        Private Function GetInvertedIfNode(
            ifNode As MultiLineIfBlockSyntax,
            document As Document,
            generator As SyntaxGenerator,
            syntaxFacts As ISyntaxFactsService,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken) As MultiLineIfBlockSyntax

            Dim ifPart = ifNode
            Dim elseBlock = ifNode.ElseBlock

            Dim ifStatement = ifNode.IfStatement

            Dim ifLeadingTrivia = ifNode.GetLeadingTrivia()
            Dim endifTrailingTrivia = ifNode.EndIfStatement.GetTrailingTrivia()
            Dim elseBlockLeadingTrivia = elseBlock.GetLeadingTrivia()
            Dim endifLeadingTrivia = ifNode.EndIfStatement.GetLeadingTrivia()

            Return ifNode _
                .WithIfStatement(ifStatement.WithCondition(DirectCast(Negate(ifStatement.Condition, generator, syntaxFacts, semanticModel, cancellationToken), ExpressionSyntax))) _
                .WithStatements(elseBlock.Statements) _
                .WithElseBlock(elseBlock.WithStatements(ifPart.Statements).WithLeadingTrivia(endifLeadingTrivia)) _
                .WithEndIfStatement(ifNode.EndIfStatement.WithTrailingTrivia(endifTrailingTrivia).WithLeadingTrivia(elseBlockLeadingTrivia)) _
                .WithLeadingTrivia(ifLeadingTrivia)
        End Function

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Invert_If
        End Function

    End Class
End Namespace
