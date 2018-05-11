' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    Partial Friend NotInheritable Class VisualBasicInvertIfCodeRefactoringProvider
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
