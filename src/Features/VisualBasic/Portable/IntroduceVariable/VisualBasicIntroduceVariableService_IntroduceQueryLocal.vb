' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    Friend Partial Class VisualBasicIntroduceVariableService
        Protected Overrides Function IntroduceQueryLocalAsync(document As SemanticDocument,
                                                         expression As ExpressionSyntax,
                                                         allOccurrences As Boolean,
                                                         cancellationToken As CancellationToken) As Task(Of Document)

            Dim newLocalNameToken = GenerateUniqueLocalName(document, expression, isConstant:=False, cancellationToken:=cancellationToken)
            Dim newLocalName = SyntaxFactory.IdentifierName(newLocalNameToken)

            Dim letClause = SyntaxFactory.LetClause(
                    SyntaxFactory.ExpressionRangeVariable(
                        SyntaxFactory.VariableNameEquals(
                            SyntaxFactory.ModifiedIdentifier(newLocalNameToken.WithAdditionalAnnotations(RenameAnnotation.Create()))),
                        expression)).WithAdditionalAnnotations(Formatter.Annotation)

            Dim oldOutermostQuery = expression.GetAncestorsOrThis(Of QueryExpressionSyntax)().LastOrDefault()
            Dim matches = FindMatches(document, expression, document, oldOutermostQuery, allOccurrences, cancellationToken)
            Dim innermostClauses = New HashSet(Of QueryClauseSyntax)(
                matches.Select(Function(expr) expr.GetAncestor(Of QueryClauseSyntax)()))

            If innermostClauses.Count = 1 Then
                ' If there was only one match, or all the matches came from the same
                ' statement, then we want to place the declaration right above that
                ' statement. Note: we special case this because the statement we are going
                ' to go above might not be in a block and we may have to generate it
                Return Task.FromResult(IntroduceQueryLocalForSingleOccurrence(
                    document, expression, newLocalName, letClause, allOccurrences, cancellationToken))
            End If

            Dim oldInnerMostCommonQuery = matches.FindInnermostCommonNode(Of QueryExpressionSyntax)()
            Dim newInnerMostQuery = Rewrite(document, expression, newLocalName, document, oldInnerMostCommonQuery, allOccurrences, cancellationToken)

            Dim allAffectedClauses = New HashSet(Of QueryClauseSyntax)(
                matches.SelectMany(Function(expr) expr.GetAncestorsOrThis(Of QueryClauseSyntax)()))

            Dim oldClauses = oldInnerMostCommonQuery.Clauses
            Dim newClauses = newInnerMostQuery.Clauses

            Dim firstClauseAffectedInQuery = oldClauses.First(AddressOf allAffectedClauses.Contains)
            Dim firstClauseAffectedIndex = oldClauses.IndexOf(firstClauseAffectedInQuery)

            Dim finalClauses = newClauses.Take(firstClauseAffectedIndex).
                                          Concat(letClause).
                                          Concat(newClauses.Skip(firstClauseAffectedIndex)).ToList()

            Dim finalQuery = newInnerMostQuery.WithClauses(SyntaxFactory.List(finalClauses))
            Dim newRoot = document.Root.ReplaceNode(oldInnerMostCommonQuery, finalQuery)

            Return Task.FromResult(document.Document.WithSyntaxRoot(newRoot))
        End Function

        Private Function IntroduceQueryLocalForSingleOccurrence(
            document As SemanticDocument,
            expression As ExpressionSyntax,
            newLocalName As NameSyntax,
            letClause As LetClauseSyntax,
            allOccurrences As Boolean,
            cancellationToken As CancellationToken) As Document

            Dim oldClause = expression.GetAncestor(Of QueryClauseSyntax)()
            Dim newClause = Rewrite(document, expression, newLocalName, document, oldClause, allOccurrences, cancellationToken)

            Dim oldQuery = DirectCast(oldClause.Parent, QueryExpressionSyntax)
            Dim newQuery = GetNewQuery(oldQuery, oldClause, newClause, letClause)

            Dim newRoot = document.Root.ReplaceNode(oldQuery, newQuery)
            Return document.Document.WithSyntaxRoot(newRoot)
        End Function

        Private Function GetNewQuery(
            oldQuery As QueryExpressionSyntax,
            oldClause As QueryClauseSyntax,
            newClause As QueryClauseSyntax,
            letClause As LetClauseSyntax) As QueryExpressionSyntax

            Dim oldClauses = oldQuery.Clauses
            Dim oldClauseIndex = oldClauses.IndexOf(oldClause)

            Dim newClauses = oldClauses.Take(oldClauseIndex).
                                        Concat(letClause).
                                        Concat(newClause).
                                        Concat(oldClauses.Skip(oldClauseIndex + 1)).ToList()

            Return oldQuery.WithClauses(SyntaxFactory.List(newClauses))
        End Function
    End Class
End Namespace
