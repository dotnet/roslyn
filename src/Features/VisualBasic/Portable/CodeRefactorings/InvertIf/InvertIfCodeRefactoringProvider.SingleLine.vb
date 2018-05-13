' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertIf), [Shared]>
    Friend NotInheritable Class VisualBasicInvertIfSingleLineCodeRefactoringProvider
        Inherits VisualBasicInvertIfCodeRefactoringProvider(Of SingleLineIfStatementSyntax)

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

        Protected Overrides Function IsElselessIfStatement(ifStatement As SingleLineIfStatementSyntax) As Boolean
            Return ifStatement.ElseClause Is Nothing
        End Function

        Protected Overrides Function CanInvert(ifStatement As SingleLineIfStatementSyntax) As Boolean
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function GetNearmostParentJumpStatementRawKind(ifStatement As SingleLineIfStatementSyntax) As Integer
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function IsEmptyStatementRange(range As (first As SyntaxNode, last As SyntaxNode)) As Boolean
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function GetIfBodyStatementRange(ifStatement As SingleLineIfStatementSyntax) As (first As SyntaxNode, last As SyntaxNode)
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function GetSubsequentStatementRange(ifStatement As SingleLineIfStatementSyntax) As IEnumerable(Of (first As SyntaxNode, last As SyntaxNode))
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
