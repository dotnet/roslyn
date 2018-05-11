' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    Partial Friend NotInheritable Class VisualBasicInvertIfCodeRefactoringProvider
        Private MustInherit Class BaseAnalyzer(Of TIfStatementSyntax As ExecutableStatementSyntax)
            Inherits Analyzer(Of TIfStatementSyntax)

            Protected Shared ReadOnly s_ifNodeAnnotation As New SyntaxAnnotation

            Protected NotOverridable Overrides Function GetTitle() As String
                Return VBFeaturesResources.Invert_If
            End Function

            Protected Overrides Function GetRootWithInvertIfStatement(document As Document,
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
    End Class
End Namespace
