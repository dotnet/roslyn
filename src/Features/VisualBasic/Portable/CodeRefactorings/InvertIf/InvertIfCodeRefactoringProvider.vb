' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.InvertIf
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertIf), [Shared]>
    Friend NotInheritable Class VisualBasicInvertIfCodeRefactoringProvider
        Inherits AbstractInvertIfCodeRefactoringProvider

        Protected Overrides Function GetIfStatement(token As SyntaxToken) As SyntaxNode
            Return If(DirectCast(token.GetAncestor(Of SingleLineIfStatementSyntax), SyntaxNode),
                      DirectCast(token.GetAncestor(Of MultiLineIfBlockSyntax), SyntaxNode))
        End Function

        Protected Overrides Function GetAnalyzer(ifStatement As SyntaxNode) As IAnalyzer
            Return If(TypeOf ifStatement Is SingleLineIfStatementSyntax,
                      DirectCast(New SingleLineIfStatementAnalyzer, IAnalyzer),
                      DirectCast(New MultiLineIfStatementAnalyzer, IAnalyzer))
        End Function

        Private MustInherit Class BaseAnalyzer(Of TIfStatementSyntax As ExecutableStatementSyntax)
            Inherits Analyzer(Of TIfStatementSyntax)

            Protected Shared ReadOnly s_ifNodeAnnotation As New SyntaxAnnotation

            Protected NotOverridable Overrides Function GetTitle() As String
                Return VBFeaturesResources.Invert_If
            End Function

            Friend Overrides Function GetRootWithInvertIfStatement(document As Document,
                                                                   semanticModel As SemanticModel,
                                                                   ifStatement As TIfStatementSyntax,
                                                                   invertIfStyle As InvertIfStyle,
                                                                   generatedJumpStatementRawKindOpt As Integer?,
                                                                   subsequenceSingleExitPointOpt As SyntaxNode,
                                                                   cancellationToken As CancellationToken) As SyntaxNode
                Dim generator = SyntaxGenerator.GetGenerator(document)
                Dim syntaxFacts = VisualBasicSyntaxFactsService.Instance

                Dim result = UpdateSemanticModel(semanticModel, semanticModel.SyntaxTree.GetRoot().ReplaceNode(ifStatement, ifStatement.WithAdditionalAnnotations(s_ifNodeAnnotation)), cancellationToken)

                Dim ifNode = DirectCast(result.Root.GetAnnotatedNodesAndTokens(s_ifNodeAnnotation).Single().AsNode(), TIfStatementSyntax)

                'In order to add parentheses for SingleLineIfStatements with commas, such as
                'Case Sub() [||]If True Then Dim x Else Return, Nothing
                'complexify the top-most statement parenting this if-statement if necessary
                Dim topMostExpression = ifNode.Ancestors().OfType(Of ExpressionSyntax).LastOrDefault()
                If topMostExpression IsNot Nothing Then
                    Dim topMostStatement = topMostExpression.Ancestors().OfType(Of StatementSyntax).FirstOrDefault()
                    If topMostStatement IsNot Nothing Then
                        Dim explicitTopMostStatement = Simplifier.Expand(topMostStatement, result.Model, document.Project.Solution.Workspace, cancellationToken:=cancellationToken)
                        result = UpdateSemanticModel(result.Model, result.Root.ReplaceNode(topMostStatement, explicitTopMostStatement), cancellationToken)
                        ifNode = DirectCast(result.Root.GetAnnotatedNodesAndTokens(s_ifNodeAnnotation).Single().AsNode(), TIfStatementSyntax)
                    End If
                End If

                semanticModel = InvertIfStatement((ifNode), document, generator, syntaxFacts, result.Model, cancellationToken)

                ' Complexify the inverted if node.
                result = (semanticModel, semanticModel.SyntaxTree.GetRoot())

                Dim invertedIfNode = result.Root.GetAnnotatedNodesAndTokens(s_ifNodeAnnotation).Single().AsNode()

                Dim explicitInvertedIfNode = Simplifier.Expand(invertedIfNode, result.Model, document.Project.Solution.Workspace, cancellationToken:=cancellationToken)
                result = UpdateSemanticModel(result.Model, result.Root.ReplaceNode(invertedIfNode, explicitInvertedIfNode), cancellationToken)

                Return result.Root
            End Function

            Protected MustOverride Function InvertIfStatement(ifNode As TIfStatementSyntax,
                                                              document As Document,
                                                              generator As SyntaxGenerator,
                                                              syntaxFacts As ISyntaxFactsService,
                                                              model As SemanticModel,
                                                              cancellationToken As CancellationToken) As SemanticModel

            Protected Shared Function UpdateSemanticModel(model As SemanticModel, root As SyntaxNode, cancellationToken As CancellationToken) As (Model As SemanticModel, Root As SyntaxNode)
                Dim newModel = model.Compilation.ReplaceSyntaxTree(model.SyntaxTree, root.SyntaxTree).GetSemanticModel(root.SyntaxTree)
                Return (newModel, newModel.SyntaxTree.GetRoot(cancellationToken))
            End Function
        End Class

        Private NotInheritable Class SingleLineIfStatementAnalyzer
            Inherits BaseAnalyzer(Of SingleLineIfStatementSyntax)

            Private Shared Function GetInvertedIfNode(
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

                Return ifNode.WithCondition(DirectCast(Negator.Negate(ifNode.Condition, generator, syntaxFacts, semanticModel, cancellationToken), ExpressionSyntax)) _
                         .WithStatements(newIfStatements) _
                         .WithElseClause(elseClause.WithStatements(ifNode.Statements).WithTrailingTrivia(elseClause.GetTrailingTrivia()))
            End Function

            Protected Overrides Function InvertIfStatement(
                originalIfNode As SingleLineIfStatementSyntax,
                document As Document,
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


            Protected Overrides Function GetHeaderSpan(ifStatement As SingleLineIfStatementSyntax) As TextSpan
                Return TextSpan.FromBounds(
                    ifStatement.IfKeyword.SpanStart,
                    ifStatement.Condition.Span.End)
            End Function

            Protected Overrides Sub AnalyzeSubsequence(semanticModel As SemanticModel, ifStatement As SingleLineIfStatementSyntax, ByRef subsequenceCount As Integer, ByRef subsequenceEndPontIsReachable As Boolean, ByRef subsequenceIsInSameBlock As Boolean, ByRef subsequenceSingleExitPointOpt As SyntaxNode, ByRef jumpStatementRawKindOpt As Integer?)
                Throw New NotImplementedException()
            End Sub

            Protected Overrides Function AnalyzeIfBodyControlFlow(semanticModel As SemanticModel, ifStatement As SingleLineIfStatementSyntax) As ControlFlowAnalysis
                Throw New NotImplementedException()
            End Function

            Protected Overrides Function GetIfBodyStatementCount(ifStatement As SingleLineIfStatementSyntax) As Integer
                Throw New NotImplementedException()
            End Function

            Protected Overrides Function IsElselessIfStatement(ifStatement As SingleLineIfStatementSyntax) As Boolean?
                Return ifStatement.ElseClause Is Nothing
            End Function
        End Class

        Private NotInheritable Class MultiLineIfStatementAnalyzer
            Inherits BaseAnalyzer(Of MultiLineIfBlockSyntax)

            Protected Overrides Sub AnalyzeSubsequence(semanticModel As SemanticModel, ifStatement As MultiLineIfBlockSyntax, ByRef subsequenceCount As Integer, ByRef subsequenceEndPontIsReachable As Boolean, ByRef subsequenceIsInSameBlock As Boolean, ByRef subsequenceSingleExitPointOpt As SyntaxNode, ByRef jumpStatementRawKindOpt As Integer?)
                Throw New NotImplementedException()
            End Sub

            Private Shared Function GetInvertedIfNode(
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

                ifNode = ifNode.Update(
                    ifStatement:=ifStatement.WithCondition(DirectCast(Negator.Negate(ifStatement.Condition, generator, syntaxFacts, semanticModel, cancellationToken), ExpressionSyntax)),
                    statements:=elseBlock.Statements,
                    elseIfBlocks:=Nothing,
                    elseBlock:=elseBlock.WithStatements(ifPart.Statements).WithLeadingTrivia(endifLeadingTrivia),
                    endIfStatement:=ifNode.EndIfStatement.WithTrailingTrivia(endifTrailingTrivia).WithLeadingTrivia(elseBlockLeadingTrivia))

                Return ifNode.WithLeadingTrivia(ifLeadingTrivia)
            End Function

            Protected Overrides Function InvertIfStatement(originalIfNode As MultiLineIfBlockSyntax, document As Document, generator As SyntaxGenerator, syntaxFacts As ISyntaxFactsService, model As SemanticModel, cancellationToken As CancellationToken) As SemanticModel
                Dim invertedIfNode = GetInvertedIfNode(originalIfNode, document, generator, syntaxFacts, model, cancellationToken)

                Dim result = UpdateSemanticModel(model, model.SyntaxTree.GetRoot().ReplaceNode(originalIfNode, invertedIfNode), cancellationToken)
                Return result.Model
            End Function

            Protected Overrides Function GetHeaderSpan(ifStatement As MultiLineIfBlockSyntax) As TextSpan
                Return TextSpan.FromBounds(
                    ifStatement.IfStatement.IfKeyword.SpanStart,
                    ifStatement.IfStatement.Condition.Span.End)
            End Function


            Protected Overrides Function AnalyzeIfBodyControlFlow(semanticModel As SemanticModel, ifStatement As MultiLineIfBlockSyntax) As ControlFlowAnalysis
                Throw New NotImplementedException()
            End Function

            Protected Overrides Function GetIfBodyStatementCount(ifStatement As MultiLineIfBlockSyntax) As Integer
                Throw New NotImplementedException()
            End Function

            Protected Overrides Function IsElselessIfStatement(ifStatement As MultiLineIfBlockSyntax) As Boolean?
                Return If(ifStatement.ElseIfBlocks.IsEmpty, ifStatement.ElseBlock Is Nothing, Nothing)
            End Function

        End Class
    End Class
End Namespace
