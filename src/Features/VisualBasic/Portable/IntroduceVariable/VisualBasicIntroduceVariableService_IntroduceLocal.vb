' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    Friend Partial Class VisualBasicIntroduceVariableService
        Protected Overrides Async Function IntroduceLocalAsync(
                document As SemanticDocument,
                expression As ExpressionSyntax,
                allOccurrences As Boolean,
                isConstant As Boolean,
                cancellationToken As CancellationToken) As Task(Of Document)

            Dim container = GetContainerToGenerateInfo(document, expression, cancellationToken)
            Dim newLocalNameToken = GenerateUniqueLocalName(
                document, expression, isConstant, container, cancellationToken)
            Dim newLocalName = SyntaxFactory.IdentifierName(newLocalNameToken)

            Dim modifier = If(isConstant, SyntaxFactory.Token(SyntaxKind.ConstKeyword), SyntaxFactory.Token(SyntaxKind.DimKeyword))
            Dim type = GetTypeSymbol(document, expression, cancellationToken)
            Dim asClause = If(type.ContainsAnonymousType(), Nothing,
                               SyntaxFactory.SimpleAsClause(type.GenerateTypeSyntax()))

            Dim declarationStatement = SyntaxFactory.LocalDeclarationStatement(
                modifiers:=SyntaxFactory.TokenList(modifier),
                declarators:=SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(newLocalNameToken.WithAdditionalAnnotations(RenameAnnotation.Create()))),
                        asClause,
                        SyntaxFactory.EqualsValue(value:=expression.WithoutTrailingTrivia().WithoutLeadingTrivia()))))

            If Not declarationStatement.GetTrailingTrivia().Any(SyntaxKind.EndOfLineTrivia) Then
                declarationStatement = declarationStatement.WithAppendedTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
            End If

            If TypeOf container Is SingleLineLambdaExpressionSyntax Then
                Return IntroduceLocalDeclarationIntoLambda(
                    document, DirectCast(container, SingleLineLambdaExpressionSyntax),
                    expression, newLocalName, declarationStatement, allOccurrences, cancellationToken)
            Else
                Return Await IntroduceLocalDeclarationIntoBlockAsync(
                    document, container, expression, newLocalName,
                    declarationStatement, allOccurrences, cancellationToken).ConfigureAwait(False)
            End If
        End Function

        Private Function GetContainerToGenerateInfo(
                document As SemanticDocument,
                expression As ExpressionSyntax,
                cancellationToken As CancellationToken) As SyntaxNode

            Dim anonymousMethodParameters = GetAnonymousMethodParameters(document, expression, cancellationToken)
            Dim lambdas = anonymousMethodParameters.SelectMany(Function(p) p.ContainingSymbol.DeclaringSyntaxReferences).
                                                    Select(Function(r) r.GetSyntax()).
                                                    OfType(Of SingleLineLambdaExpressionSyntax).
                                                    Where(Function(lambda) lambda.Kind = SyntaxKind.SingleLineFunctionLambdaExpression).
                                                    ToSet()

            Dim parentLambda = GetParentLambda(expression, lambdas)
            If parentLambda IsNot Nothing Then
                Return parentLambda
            End If

            Return expression.GetContainingExecutableBlocks().LastOrDefault()
        End Function

        Private Function IntroduceLocalDeclarationIntoLambda(
                document As SemanticDocument,
                oldLambda As SingleLineLambdaExpressionSyntax,
                expression As ExpressionSyntax,
                newLocalName As IdentifierNameSyntax,
                declarationStatement As StatementSyntax,
                allOccurrences As Boolean,
                cancellationToken As CancellationToken) As Document

            Dim oldBody = DirectCast(oldLambda.Body, ExpressionSyntax)

            Dim rewrittenBody = Rewrite(
                document, expression, newLocalName, document, oldBody, allOccurrences, cancellationToken)

            Dim statements = {declarationStatement, SyntaxFactory.ReturnStatement(rewrittenBody)}

            Dim newLambda As ExpressionSyntax = SyntaxFactory.MultiLineFunctionLambdaExpression(
                    oldLambda.SubOrFunctionHeader,
                    SyntaxFactory.List(statements),
                    SyntaxFactory.EndFunctionStatement()).WithAdditionalAnnotations(Formatter.Annotation)

            Dim newRoot = document.Root.ReplaceNode(oldLambda, newLambda)
            Return document.Document.WithSyntaxRoot(newRoot)
        End Function

        Private Function GetParentLambda(expression As ExpressionSyntax,
                                         lambdas As ISet(Of SingleLineLambdaExpressionSyntax)) As SingleLineLambdaExpressionSyntax
            Dim current = expression
            While current IsNot Nothing
                Dim parent = TryCast(current.Parent, SingleLineLambdaExpressionSyntax)
                If parent IsNot Nothing Then
                    If lambdas.Contains(parent) Then
                        Return parent
                    End If
                End If

                current = TryCast(current.Parent, ExpressionSyntax)
            End While

            Return Nothing
        End Function

        Private Async Function IntroduceLocalDeclarationIntoBlockAsync(
                document As SemanticDocument,
                container As SyntaxNode,
                expression As ExpressionSyntax,
                newLocalName As NameSyntax,
                declarationStatement As LocalDeclarationStatementSyntax,
                allOccurrences As Boolean,
                cancellationToken As CancellationToken) As Task(Of Document)

            Dim localAnnotation = New SyntaxAnnotation()
            declarationStatement = declarationStatement.WithAdditionalAnnotations(Formatter.Annotation, localAnnotation)

            Dim oldOutermostBlock = container
            If oldOutermostBlock.IsSingleLineExecutableBlock() Then
                oldOutermostBlock = oldOutermostBlock.Parent
            End If

            Dim matches = FindMatches(document, expression, document, oldOutermostBlock, allOccurrences, cancellationToken)

            Dim complexified = Await ComplexifyParentingStatements(document, matches, cancellationToken).ConfigureAwait(False)
            document = complexified.newSemanticDocument
            matches = complexified.newMatches

            ' Our original expression should have been one of the matches, which were tracked as part
            ' of complexification, so we can retrieve the latest version of the expression here.
            expression = document.Root.GetCurrentNodes(expression).First()

            Dim innermostStatements = New HashSet(Of StatementSyntax)(matches.Select(Function(expr) expr.GetAncestorOrThis(Of StatementSyntax)()))
            If innermostStatements.Count = 1 Then
                Return IntroduceLocalForSingleOccurrenceIntoBlock(
                    document, expression, newLocalName, declarationStatement, localAnnotation, allOccurrences, cancellationToken)
            End If

            Dim oldInnerMostCommonBlock = matches.FindInnermostCommonExecutableBlock()
            Dim allAffectedStatements = New HashSet(Of StatementSyntax)(matches.SelectMany(Function(expr) expr.GetAncestorsOrThis(Of StatementSyntax)()))
            Dim firstStatementAffectedInBlock = oldInnerMostCommonBlock.GetExecutableBlockStatements().First(AddressOf allAffectedStatements.Contains)
            Dim firstStatementAffectedIndex = oldInnerMostCommonBlock.GetExecutableBlockStatements().IndexOf(firstStatementAffectedInBlock)
            Dim newInnerMostBlock = Rewrite(document, expression, newLocalName, document, oldInnerMostCommonBlock, allOccurrences, cancellationToken)

            Dim statements = newInnerMostBlock.GetExecutableBlockStatements().Insert(firstStatementAffectedIndex, declarationStatement)
            Dim finalInnerMostBlock = oldInnerMostCommonBlock.ReplaceStatements(statements, Formatter.Annotation)

            Dim newRoot = document.Root.ReplaceNode(oldInnerMostCommonBlock, finalInnerMostBlock)
            Return document.Document.WithSyntaxRoot(newRoot)
        End Function

        Private Function IntroduceLocalForSingleOccurrenceIntoBlock(
                semanticDocument As SemanticDocument,
                expression As ExpressionSyntax,
                localName As NameSyntax,
                localDeclaration As LocalDeclarationStatementSyntax,
                localAnnotation As SyntaxAnnotation,
                allOccurrences As Boolean,
                cancellationToken As CancellationToken) As Document

            Dim oldStatement = expression.GetAncestorsOrThis(Of StatementSyntax)().Where(
                Function(s) s.Parent.IsExecutableBlock() AndAlso s.Parent.GetExecutableBlockStatements().Contains(s)).First()
            Dim newStatement = Rewrite(semanticDocument, expression, localName, semanticDocument, oldStatement, allOccurrences, cancellationToken)

            localDeclaration = localDeclaration.WithLeadingTrivia(newStatement.GetLeadingTrivia())
            newStatement = newStatement.WithLeadingTrivia(newStatement.GetLeadingTrivia().Where(Function(trivia) trivia.IsKind(SyntaxKind.WhitespaceTrivia)))

            Dim oldBlock = oldStatement.Parent

            If oldBlock.IsSingleLineExecutableBlock() Then
                Dim tree = semanticDocument.SyntaxTree
                Dim statements = SyntaxFactory.List({localDeclaration, newStatement})
                Dim newRoot = tree.ConvertSingleLineToMultiLineExecutableBlock(oldBlock, statements, Formatter.Annotation)

                Return semanticDocument.Document.WithSyntaxRoot(newRoot)
            Else
                Dim statementIndex = oldBlock.GetExecutableBlockStatements().IndexOf(oldStatement)
                Dim newStatements =
                    oldBlock.GetExecutableBlockStatements().Replace(oldStatement, newStatement).Insert(statementIndex, localDeclaration)

                Dim newBlock = oldBlock.ReplaceStatements(newStatements)
                Dim oldRoot = semanticDocument.Root
                Dim newRoot = oldRoot.ReplaceNode(oldBlock, newBlock)

                Return semanticDocument.Document.WithSyntaxRoot(newRoot)
            End If
        End Function

    End Class
End Namespace
