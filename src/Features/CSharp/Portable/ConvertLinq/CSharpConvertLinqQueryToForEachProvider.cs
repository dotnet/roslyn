// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq
{
    internal sealed class CSharpConvertLinqQueryToLinqMethodProvider : AbstractConvertLinqProvider
    {
        private static readonly TypeSyntax VarNameIdentifier = SyntaxFactory.IdentifierName("var");

        protected override IAnalyzer CreateAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken)
            => new CSharpAnalyzer(semanticModel, cancellationToken);

        private sealed class CSharpAnalyzer : AnalyzerBase<QueryExpressionSyntax, SyntaxNode, SyntaxNode>
        {
            public CSharpAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken)
                : base(semanticModel, cancellationToken)
            {
            }

            protected override string Title => CSharpFeaturesResources.Convert_LINQ_query_to_foreach;

            protected override bool TryConvert(QueryExpressionSyntax source, out DocumentUpdate documentUpdate)
            {
                if (_semanticModel.GetDiagnostics(source.Span, _cancellationToken).Any() ||
                    source.DescendantTrivia().Any(trivia => trivia.MatchesKind(
                        SyntaxKind.SingleLineCommentTrivia,
                        SyntaxKind.MultiLineCommentTrivia,
                        SyntaxKind.MultiLineDocumentationCommentTrivia) ||
                        source.ContainsDirectives))
                {
                    documentUpdate = default;
                    return false;
                }

                return TryConvertInternal(source, source.Parent, out documentUpdate);
            }

            private bool TryConvertInternal(QueryExpressionSyntax source, SyntaxNode parent, out DocumentUpdate documentUpdate)
            {
                switch (parent.Kind())
                {
                    case SyntaxKind.ReturnStatement:
                        return TryConvertIfInReturnStatement(source, (ReturnStatementSyntax)parent, out documentUpdate);
                    case SyntaxKind.ForEachStatement:
                        return TryConvertIfInForEach(source, (ForEachStatementSyntax)parent, out documentUpdate);
                    case SyntaxKind.EqualsValueClause:
                    case SyntaxKind.SimpleAssignmentExpression:
                        return TryReplaceWithLocalFunction(source, parent, out documentUpdate);
                    case SyntaxKind.ParenthesizedExpression:
                        return TryConvertInternal(source, parent.Parent, out documentUpdate);
                    case SyntaxKind.SimpleMemberAccessExpression:
                        return TryConvertIfInMemberAccessExpression(source, (MemberAccessExpressionSyntax)parent, out documentUpdate);
                    default:
                        documentUpdate = default;
                        return false;
                }
            }

            private bool TryConvertIfInMemberAccessExpression(
               QueryExpressionSyntax source,
               MemberAccessExpressionSyntax memberAccessExpression,
               out DocumentUpdate documentUpdate)
            {
                if (memberAccessExpression.Parent is InvocationExpressionSyntax invocationExpression)
                {
                    switch (memberAccessExpression.Name.Identifier.ValueText)
                    {
                        case nameof(Enumerable.ToList):
                            return TryConvertIfInToListInvocation(source, invocationExpression, out documentUpdate);
                        case nameof(Enumerable.Count):
                            return TryConvertIfInCountInvocation(source, invocationExpression, out documentUpdate);
                        default:
                            return TryReplaceWithLocalFunction(source, memberAccessExpression, out documentUpdate);
                    }
                }

                documentUpdate = default;
                return false;
            }

            private bool TryConvertIfInCountInvocation(
                QueryExpressionSyntax source,
                InvocationExpressionSyntax invocationExpression,
                out DocumentUpdate documentUpdate)
            {
                if (_semanticModel.GetSymbolInfo(invocationExpression, _cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.Parameters.Length == 0 && methodSymbol.ReturnType?.SpecialType == SpecialType.System_Int32)
                {
                    return TryConvertIfInInvocation(
                            source,
                            invocationExpression,
                            (variableIdentifier, expression) => SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, variableIdentifier)), // Generating 'count++;'
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)),
                            "count",
                            out documentUpdate);
                }

                documentUpdate = default;
                return false;
            }

            private bool TryConvertIfInToListInvocation(
                QueryExpressionSyntax source,
                InvocationExpressionSyntax invocationExpression,
                out DocumentUpdate documentUpdate)
            {
                if (_semanticModel.GetSymbolInfo(invocationExpression, _cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.Parameters.Length == 0)
                {
                    return TryConvertIfInInvocation(
                              source,
                              invocationExpression,
                              (listIdentifier, expression) => SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        listIdentifier,
                                        SyntaxFactory.IdentifierName(nameof(IList.Add))),
                                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(expression))))),
                              SyntaxFactory.ObjectCreationExpression(
                                  methodSymbol.GenerateReturnTypeSyntax().WithAdditionalAnnotations(Simplifier.Annotation),
                                  SyntaxFactory.ArgumentList(),
                                  null),
                               "list",
                              out documentUpdate);
                }

                documentUpdate = default;
                return false;
            }

            private bool TryConvertIfInInvocation(
                QueryExpressionSyntax source,
                InvocationExpressionSyntax invocationExpression,
                Func<ExpressionSyntax, ExpressionSyntax, StatementSyntax> leafExpressionCreationMethod,
                ExpressionSyntax initializer,
                string variableName,
                out DocumentUpdate documentUpdate)
            {
                var parentStatement = invocationExpression.GetAncestorOrThis<StatementSyntax>();
                if (parentStatement != null &&
                    !parentStatement.Equals(default) &&
                    TryConvertIfInInvocationInternal(
                        source,
                        invocationExpression,
                        parentStatement,
                        initializer,
                        variableName,
                        out var variable,
                        out var documentCreationMethod) &&
                      TryConvertQueryExpression(
                          source,
                          expression => leafExpressionCreationMethod(variable, expression),
                          out var statement))
                {
                    documentUpdate = documentCreationMethod(statement);
                    return true;
                }

                documentUpdate = default;
                return false;
            }

            private bool TryConvertIfInInvocationInternal(
                QueryExpressionSyntax source,
                InvocationExpressionSyntax invocationExpression,
                StatementSyntax parentStatement,
                ExpressionSyntax initializer,
                string variableName,
                out ExpressionSyntax variable,
                out Func<StatementSyntax, DocumentUpdate> documentCreationMethod)
            {
                var invocationParent = invocationExpression.Parent;
                switch (invocationParent.Kind())
                {
                    case SyntaxKind.EqualsValueClause:
                        if (invocationParent.Parent is VariableDeclaratorSyntax variableDeclarator)
                        {
                            variable = SyntaxFactory.IdentifierName(variableDeclarator.Identifier);
                            documentCreationMethod = s =>
                            new DocumentUpdate(
                                parentStatement,
                                new[] { parentStatement.ReplaceNode(invocationExpression, initializer), s });
                            return true;
                        }
                        break;
                    case SyntaxKind.SimpleAssignmentExpression:
                        variable = ((AssignmentExpressionSyntax)invocationParent).Left;
                        documentCreationMethod = s =>
                            new DocumentUpdate(
                                parentStatement,
                                new[] { parentStatement.ReplaceNode(invocationExpression, initializer), s });
                        return true;
                    case SyntaxKind.ReturnStatement:
                        string freeVariableName = GetFreeSymbolName(variableName, source.GetLocation().SourceSpan.Start);
                        var identifierName = SyntaxFactory.IdentifierName(freeVariableName);
                        var variableDeclaration =
                            SyntaxFactory.LocalDeclarationStatement(
                                SyntaxFactory.VariableDeclaration(
                                    VarNameIdentifier,
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.VariableDeclarator(
                                            identifierName.Identifier,
                                            argumentList: null,
                                            SyntaxFactory.EqualsValueClause(initializer)))));

                        variable = identifierName;
                        documentCreationMethod = s => new DocumentUpdate(
                            parentStatement,
                            new[] { variableDeclaration, s, SyntaxFactory.ReturnStatement(identifierName) });
                        return true;
                        // SyntaxKind.ArrowExpressionClause is not supported
                }

                documentCreationMethod = default;
                variable = default;
                return false;
            }

            private bool TryReplaceWithLocalFunction(QueryExpressionSyntax source, SyntaxNode parent, out DocumentUpdate documentUpdate)
            {
                var parentStatement = parent.GetAncestorOrThis<StatementSyntax>();
                if (parentStatement != null && !parentStatement.Equals(default))
                {
                    string localFunctionName = GetFreeSymbolName("localFunction", source.GetLocation().SourceSpan.Start);
                    StatementSyntax internalNodeMethod(ExpressionSyntax expression) => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, expression);
                    if (TryConvertQueryExpression(source, internalNodeMethod, out var convertedFromQueryExpression))
                    {
                        if (TryGetTypeSyntax(source, out var typeSyntax))
                        {
                            var localFunctionDeclaration = SyntaxFactory.LocalFunctionStatement(
                                modifiers: default,
                                returnType: typeSyntax.WithAdditionalAnnotations(Simplifier.SpecialTypeAnnotation),
                                identifier: SyntaxFactory.Identifier(localFunctionName),
                                typeParameterList: null,
                                parameterList: SyntaxFactory.ParameterList(),
                                constraintClauses: default,
                                body: SyntaxFactory.Block(
                                    SyntaxFactory.Token(
                                        SyntaxFactory.TriviaList(),
                                        SyntaxKind.OpenBraceToken,
                                        SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine(Environment.NewLine))),
                                    SyntaxFactory.SingletonList(convertedFromQueryExpression),
                                    SyntaxFactory.Token(SyntaxKind.CloseBraceToken)),
                                expressionBody: null);

                            var localFunctionInvocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(localFunctionName));
                            SyntaxNode newParentExpressionStatement = parentStatement.ReplaceNode(source, localFunctionInvocation);
                            documentUpdate = new DocumentUpdate(parentStatement, new[] { localFunctionDeclaration, newParentExpressionStatement });
                            return true;
                        }
                    }
                }

                documentUpdate = default;
                return false;
            }

            private bool TryGetTypeSyntax(ExpressionSyntax expression, out TypeSyntax typeSyntax)
            {
                var typeSymbol = _semanticModel.GetTypeInfo(expression, _cancellationToken).Type;
                if (typeSymbol.TypeKind == TypeKind.Error ||
                    typeSymbol.ContainsAnonymousType())
                {
                    typeSyntax = default;
                    return false;
                }

                typeSyntax = typeSymbol.GenerateTypeSyntax();
                return true;
            }

            private string GetFreeSymbolName(string prefix, int position)
                => Shared.Utilities.NameGenerator.GenerateUniqueName(
                    prefix,
                    _semanticModel.LookupSymbols(position).Select(symbol => symbol.Name).ToImmutableHashSet(),
                    StringComparer.CurrentCulture);

            private bool TryConvertIfInForEach(QueryExpressionSyntax source, ForEachStatementSyntax forEachStatement, out DocumentUpdate documentUpdate)
            {
                var statement = forEachStatement.Statement;
                StatementSyntax internalNodeMethod(ExpressionSyntax expression)
                {
                    if (expression is IdentifierNameSyntax identifier &&
                        identifier.Identifier.ValueText == forEachStatement.Identifier.ValueText)
                    {
                        return statement;
                    }
                    else
                    {
                        var declaration = SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                VarNameIdentifier,
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(
                                        forEachStatement.Identifier,
                                        argumentList: null,
                                        SyntaxFactory.EqualsValueClause(expression)))));
                        return AddToBlockTop(declaration, statement);
                    }
                }

                if (TryConvertQueryExpression(source, internalNodeMethod, out StatementSyntax newNode))
                {
                    documentUpdate = new DocumentUpdate(forEachStatement, newNode);
                    return true;
                }

                documentUpdate = default;
                return false;
            }

            private bool TryConvertIfInReturnStatement(QueryExpressionSyntax source, ReturnStatementSyntax returnStatement, out DocumentUpdate documentUpdate)
            {
                StatementSyntax internalNodeMethod(ExpressionSyntax expression) => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, expression);
                if (TryConvertQueryExpression(source, internalNodeMethod, out var newNode))
                {
                    documentUpdate = new DocumentUpdate(returnStatement, newNode);
                    return true;
                }

                documentUpdate = default;
                return false;
            }

            private bool TryConvertQueryExpression(
                QueryExpressionSyntax source,
                Func<ExpressionSyntax, StatementSyntax> leafExpressionCreationMethod,
                out StatementSyntax statement)
            {
                ProcessingState processingState = new ProcessingState(leafExpressionCreationMethod);
                if (TryProcessFromClause(source.FromClause, processingState) &&
                    TryProcessQueryBody(source.Body, processingState))
                {
                    // Executes syntax building methods from bottom to the top of the tree.
                    statement = processingState.ExecuteMethods();
                    return true;
                }

                statement = default;
                return false;
            }

            private bool TryProcessQueryBody(
                QueryBodySyntax queryBody,
                ProcessingState processingState)
            {
                foreach (var queryClause in queryBody.Clauses)
                {
                    if (!TryProcessQueryClause(queryClause, processingState))
                    {
                        return false;
                    }
                }

                // GroupClause is not supported by the conversion
                return queryBody.SelectOrGroup is SelectClauseSyntax selectClause &&
                    TryProcessSelectClause(selectClause, queryBody.Continuation, processingState);
            }

            private bool TryProcessQueryClause(
                QueryClauseSyntax queryClause,
                ProcessingState processingState)
            {
                switch (queryClause.Kind())
                {
                    case SyntaxKind.WhereClause:
                        return TryProcessWhereClause((WhereClauseSyntax)queryClause, processingState);
                    case SyntaxKind.LetClause:
                        return TryProcessLetClause((LetClauseSyntax)queryClause, processingState);
                    case SyntaxKind.FromClause:
                        return TryProcessFromClause((FromClauseSyntax)queryClause, processingState);
                    case SyntaxKind.JoinClause:
                        return TryProcessJoinClause((JoinClauseSyntax)queryClause, processingState);
                }

                // OrderBy is not supported by foreach.
                return false;
            }

            private bool TryProcessSelectClause(
                SelectClauseSyntax selectClause,
                QueryContinuationSyntax queryContinuation,
                ProcessingState processingState)
            {
                // TODO semantic check for selectClause.Expression
                // End of the queue
                if (queryContinuation == null)
                {
                    processingState.AddStatementProcessingMethod(
                        s => WrapWithBlock(processingState.LeafExpressionCreationMethod(selectClause.Expression)));
                    return true;
                }
                else
                {
                    SymbolInfo symbolInfo = _semanticModel.GetSymbolInfo(queryContinuation, _cancellationToken);
                    if (symbolInfo.Symbol == null ||
                        symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                        methodSymbol.Parameters.Length == 1)
                    {
                        processingState.AddVariableName(queryContinuation.Identifier.ValueText);
                        processingState.AddStatementProcessingMethod(
                            s => AddToBlockTop(
                                 SyntaxFactory.LocalDeclarationStatement(
                                    SyntaxFactory.VariableDeclaration(
                                        VarNameIdentifier,
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.VariableDeclarator(
                                                queryContinuation.Identifier,
                                                argumentList: null,
                                                SyntaxFactory.EqualsValueClause(selectClause.Expression))))), s));

                        return TryProcessQueryBody(queryContinuation.Body, processingState);
                    }
                }

                return false;
            }

            private bool TryProcessWhereClause(WhereClauseSyntax whereClause, ProcessingState processingState)
            {
                QueryClauseInfo queryClauseInfo = _semanticModel.GetQueryClauseInfo(whereClause, _cancellationToken);
                if (queryClauseInfo.OperationInfo.Symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.Parameters.Length == 1)
                {
                    processingState.AddStatementProcessingMethod(
                         s => SyntaxFactory.Block(
                             SyntaxFactory.IfStatement(
                                 ProcessCondition((whereClause).Condition),
                                 s)));

                    return true;
                }

                return false;
            }

            private bool TryProcessFromClause(FromClauseSyntax fromClause, ProcessingState processingState)
            {
                QueryClauseInfo queryClauseInfo = _semanticModel.GetQueryClauseInfo(fromClause, _cancellationToken);
                if (queryClauseInfo.OperationInfo.Symbol == null || // First FromClause may have no OperationInfo symbols.
                    (queryClauseInfo.OperationInfo.Symbol is IMethodSymbol methodSymbol && methodSymbol.Parameters.Length == 2))
                {
                    processingState.AddVariableName(fromClause.Identifier.ValueText);
                    processingState.AddStatementProcessingMethod(
                        s => SyntaxFactory.ForEachStatement(
                                    fromClause.Type ?? VarNameIdentifier,
                                    fromClause.Identifier,
                                    fromClause.Expression.WithoutTrivia(), WrapWithBlock(s)));
                    return true;
                }

                return false;
            }

            private bool TryProcessLetClause(LetClauseSyntax letClause, ProcessingState processingState)
            {
                QueryClauseInfo queryClauseInfo = _semanticModel.GetQueryClauseInfo(letClause, _cancellationToken);
                if (queryClauseInfo.OperationInfo.Symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.Parameters.Length == 1)
                {
                    processingState.AddVariableName(letClause.Identifier.ValueText);
                    processingState.AddStatementProcessingMethod(
                        s => AddToBlockTop(
                                SyntaxFactory.LocalDeclarationStatement(
                                    SyntaxFactory.VariableDeclaration(
                                        VarNameIdentifier,
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.VariableDeclarator(
                                                letClause.Identifier,
                                                null,
                                                SyntaxFactory.EqualsValueClause(letClause.Expression))))),
                                s));

                    return true;
                }

                return false;
            }

            private bool TryProcessJoinClause(JoinClauseSyntax joinClause, ProcessingState processingState)
            {
                QueryClauseInfo queryClauseInfo = _semanticModel.GetQueryClauseInfo(joinClause, _cancellationToken);
                if (queryClauseInfo.OperationInfo.Symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.Parameters.Length == 4)
                {
                    processingState.AddVariableName(joinClause.Identifier.ValueText);
                    if (joinClause.Into != null)
                    {
                        var intoDeclaration =
                            SyntaxFactory.LocalDeclarationStatement(
                                SyntaxFactory.VariableDeclaration(
                                    VarNameIdentifier,
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.VariableDeclarator(
                                            joinClause.Into.Identifier,
                                            argumentList: null,
                                            SyntaxFactory.EqualsValueClause(
                                                SyntaxFactory.AnonymousObjectCreationExpression(
                                                    SyntaxFactory.SeparatedList(
                                                        processingState.VariableNames.Select(vn => CreateAnonymousObjectMemberDeclarator(vn)))))))));
                        processingState.AddStatementProcessingMethod(s => CreateJoinStatement(joinClause, AddToBlockTop(intoDeclaration, s)));
                    }
                    else
                    {
                        processingState.AddStatementProcessingMethod(s => CreateJoinStatement(joinClause, s));
                    }

                    return true;
                }

                return false;
            }

            private static BlockSyntax AddToBlockTop(StatementSyntax newStatement, StatementSyntax statement)
            {
                if (statement is BlockSyntax block)
                {
                    return SyntaxFactory.Block(new[] { newStatement }.Concat(block.Statements));
                }
                else
                {
                    return SyntaxFactory.Block(newStatement, statement);
                }
            }

            private static BlockSyntax WrapWithBlock(StatementSyntax statement)
                => statement is BlockSyntax block ? block : SyntaxFactory.Block(statement);

            private static ExpressionSyntax ProcessCondition(ExpressionSyntax expression)
            {
                if (expression.Kind() == SyntaxKind.ParenthesizedExpression)
                {
                    expression = ((ParenthesizedExpressionSyntax)expression).Expression;
                }

                return expression.WithoutTrivia();
            }

            private static AnonymousObjectMemberDeclaratorSyntax CreateAnonymousObjectMemberDeclarator(string name)
                => SyntaxFactory.AnonymousObjectMemberDeclarator(
                    SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(name)), SyntaxFactory.IdentifierName(name));

            private static StatementSyntax CreateJoinStatement(JoinClauseSyntax joinClause, StatementSyntax statement)
            {
                return SyntaxFactory.Block(
                                SyntaxFactory.ForEachStatement(
                                    joinClause.Type ?? VarNameIdentifier,
                                    joinClause.Identifier,
                                    joinClause.InExpression,
                                    SyntaxFactory.Block(
                                        SyntaxFactory.IfStatement(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.ParenthesizedExpression(joinClause.LeftExpression),
                                                    SyntaxFactory.IdentifierName(nameof(object.Equals))),
                                                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(joinClause.RightExpression.WithoutTrailingTrivia())))),
                                            statement))));
            }

            private class ProcessingState
            {
                public ImmutableArray<string> VariableNames { get; private set; }

                // Each clause generates a method converting StatementSyntax into StatementSyntax.
                public Stack<Func<StatementSyntax, StatementSyntax>> StatementProcessingMethods { get; }

                public Func<ExpressionSyntax, StatementSyntax> LeafExpressionCreationMethod { get; }

                public ProcessingState(Func<ExpressionSyntax, StatementSyntax> leafExpressionCreationMethod)
                {
                    VariableNames = ImmutableArray<string>.Empty;
                    StatementProcessingMethods = new Stack<Func<StatementSyntax, StatementSyntax>>();
                    LeafExpressionCreationMethod = leafExpressionCreationMethod;
                }

                public void AddVariableName(string variableName) =>
                    VariableNames = VariableNames.Concat(variableName);

                public void AddStatementProcessingMethod(Func<StatementSyntax, StatementSyntax> method) =>
                    StatementProcessingMethods.Push(method);

                public StatementSyntax ExecuteMethods()
                {
                    StatementSyntax statement = default;
                    while (StatementProcessingMethods.Any())
                    {
                        statement = StatementProcessingMethods.Pop()(statement);
                    }

                    return statement;
                }
            }
        }
    }
}
