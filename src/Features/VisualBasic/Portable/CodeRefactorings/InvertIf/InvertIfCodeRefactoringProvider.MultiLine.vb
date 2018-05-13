' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertIf), [Shared]>
    Friend NotInheritable Class VisualBasicInvertMultiLineIfCodeRefactoringProvider
        Inherits VisualBasicInvertIfCodeRefactoringProvider(Of MultiLineIfBlockSyntax)

        Private Shared Function GetInvertedIfNode(
                ifNode As MultiLineIfBlockSyntax,
                document As Document,
                generator As SyntaxGenerator,
                syntaxFacts As ISyntaxFactsService,
                semanticModel As SemanticModel,
                negatedExpression As ExpressionSyntax,
                cancellationToken As CancellationToken) As MultiLineIfBlockSyntax

            Dim ifPart = ifNode
            Dim elseBlock = ifNode.ElseBlock

            Dim ifStatement = ifNode.IfStatement

            Dim ifLeadingTrivia = ifNode.GetLeadingTrivia()
            Dim endifTrailingTrivia = ifNode.EndIfStatement.GetTrailingTrivia()
            Dim elseBlockLeadingTrivia = elseBlock.GetLeadingTrivia()
            Dim endifLeadingTrivia = ifNode.EndIfStatement.GetLeadingTrivia()

            ifNode = ifNode.Update(
                    ifStatement:=ifStatement.WithCondition(DirectCast(negatedExpression, ExpressionSyntax)),
                    statements:=elseBlock.Statements,
                    elseIfBlocks:=Nothing,
                    elseBlock:=elseBlock.WithStatements(ifPart.Statements).WithLeadingTrivia(endifLeadingTrivia),
                    endIfStatement:=ifNode.EndIfStatement.WithTrailingTrivia(endifTrailingTrivia).WithLeadingTrivia(elseBlockLeadingTrivia))

            Return ifNode.WithLeadingTrivia(ifLeadingTrivia)
        End Function

        Protected Overrides Function InvertIfStatement(originalIfNode As MultiLineIfBlockSyntax, document As Document, generator As SyntaxGenerator, syntaxFacts As ISyntaxFactsService, model As SemanticModel, negatedExpression As ExpressionSyntax, cancellationToken As CancellationToken) As SemanticModel
            Dim invertedIfNode = GetInvertedIfNode(originalIfNode, document, generator, syntaxFacts, model, negatedExpression, cancellationToken)
            Dim result = UpdateSemanticModel(model, model.SyntaxTree.GetRoot().ReplaceNode(originalIfNode, invertedIfNode), cancellationToken)
            Return result.Model
        End Function

        Protected Overrides Function GetHeaderSpan(ifStatement As MultiLineIfBlockSyntax) As TextSpan
            Return TextSpan.FromBounds(
                    ifStatement.IfStatement.IfKeyword.SpanStart,
                    ifStatement.IfStatement.Condition.Span.End)
        End Function

        Protected Overrides Function IsElselessIfStatement(ifStatement As MultiLineIfBlockSyntax) As Boolean
            Return ifStatement.ElseBlock Is Nothing
        End Function

        Protected Overrides Function CanInvert(ifStatement As MultiLineIfBlockSyntax) As Boolean
            Return ifStatement.ElseIfBlocks.IsEmpty
        End Function

        Protected Overrides Function GetNearmostParentJumpStatementRawKind(ifStatement As MultiLineIfBlockSyntax) As Integer
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function IsEmptyStatementRange(range As (first As SyntaxNode, last As SyntaxNode)) As Boolean
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function GetIfBodyStatementRange(ifStatement As MultiLineIfBlockSyntax) As (first As SyntaxNode, last As SyntaxNode)
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function GetSubsequentStatementRange(ifStatement As MultiLineIfBlockSyntax) As IEnumerable(Of (first As SyntaxNode, last As SyntaxNode))
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function GetIfCondition(ifStatement As MultiLineIfBlockSyntax) As SyntaxNode
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
