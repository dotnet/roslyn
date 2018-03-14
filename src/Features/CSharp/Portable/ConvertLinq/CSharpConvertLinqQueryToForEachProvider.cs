// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq
{
    internal sealed class CSharpConvertLinqQueryToLinqMethodProvider : AbstractConvertLinqProvider
    {
        protected override IAnalyzer CreateAnalyzer(SemanticModel semanticModel, Document document, CancellationToken cancellationToken)
            => new CSharpAnalyzer(semanticModel, document, cancellationToken);

        private sealed class CSharpAnalyzer : AnalyzerBase<QueryExpressionSyntax, SyntaxNode, SyntaxNode>
        {
            public CSharpAnalyzer(SemanticModel semanticModel, Document document, CancellationToken cancellationToken)
                : base(semanticModel, document, cancellationToken)
            {
            }

            protected override string Title => CSharpFeaturesResources.Convert_linq_query_to_foreach;

            protected override bool TryConvert(QueryExpressionSyntax source, out DocumentUpdate documentUpdate)
            {
                switch (source.Parent.Kind())
                {
                    case SyntaxKind.ReturnStatement:
                        return TryConvertInReturnStatement(source, out documentUpdate);
                    case SyntaxKind.ForEachStatement:
                        return TryConvertInForEach(source, out documentUpdate);
                    case SyntaxKind.EqualsValueClause:
                        return TryReplaceWithLocalFunction(source, SyntaxKind.LocalDeclarationStatement, out documentUpdate);
                    case SyntaxKind.SimpleAssignmentExpression:
                    case SyntaxKind.MultiplyAssignmentExpression:
                        return TryReplaceWithLocalFunction(source, SyntaxKind.ExpressionStatement, out documentUpdate);
                    case SyntaxKind.ParenthesizedExpression:
                        return TryConvertInParenthesizedExpression(source, out documentUpdate);
                    default:
                        documentUpdate = null;
                        return false;
                }
            }

            private bool TryConvertInParenthesizedExpression(QueryExpressionSyntax source, out DocumentUpdate documentUpdate)
            {
                // TODO any other cases?
                var parenthesizedExpression = (ParenthesizedExpressionSyntax)source.Parent;
                if (parenthesizedExpression.Parent.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                {
                    var memberAccessExpression = (MemberAccessExpressionSyntax)parenthesizedExpression.Parent;

                    Func<ExpressionSyntax, ExpressionSyntax, ExpressionSyntax> insideLoopExpression;
                    ExpressionSyntax initializer;
                    string variableNamePrefix;
                    switch (memberAccessExpression.Name.Identifier.ValueText)
                    {
                        case nameof(Enumerable.ToList):
                            if (TryGetTypeSyntax(source, out var typeSyntax))
                            {
                                insideLoopExpression = (ExpressionSyntax listIdentifier, ExpressionSyntax expression) =>
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression, // TODO why this kind???
                                                    listIdentifier,
                                                    SyntaxFactory.IdentifierName("Add")), // TODO get "Add" from nameof something
                                                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(expression)))
                                                );
                                initializer = SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.GenericName(
                                        SyntaxFactory.Identifier("List"),
                                        ((GenericNameSyntax)typeSyntax).TypeArgumentList), // / TODO "List"?
                                    SyntaxFactory.ArgumentList(), null);
                                variableNamePrefix = "list";
                            }
                            else
                            {
                                documentUpdate = null;
                                return false; // refactor
                            }
                            break;
                        case nameof(Enumerable.Count):
                            variableNamePrefix = "count";
                            insideLoopExpression = (ExpressionSyntax variableIdentifier, ExpressionSyntax expression) =>
                                    SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, variableIdentifier);
                            initializer = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0));
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    return TryConvertInInvocation(
                              source,
                             (ExpressionSyntax variableIdentifier, ExpressionSyntax expression) => new[] { SyntaxFactory.ExpressionStatement(insideLoopExpression(variableIdentifier, expression)) },
                              initializer,
                              variableNamePrefix,
                              out documentUpdate);

                }

                throw new NotImplementedException();
            }

            private bool TryConvertInInvocation(
                QueryExpressionSyntax source,
                Func<ExpressionSyntax, ExpressionSyntax, IEnumerable<StatementSyntax>> insertMethod,
                ExpressionSyntax initializer,
                string variableName,
                out DocumentUpdate documentUpdate)
            {
                var parenthesizedExpression = (ParenthesizedExpressionSyntax)source.Parent;
                if (parenthesizedExpression.Parent.Parent.Kind() == SyntaxKind.InvocationExpression)
                {
                    switch (parenthesizedExpression.Parent.Parent.Parent.Kind())
                    {
                        case SyntaxKind.EqualsValueClause:
                        case SyntaxKind.MultiplyAssignmentExpression:
                        case SyntaxKind.SimpleAssignmentExpression:
                            if (TryGetVariableName(parenthesizedExpression.Parent.Parent.Parent, out var identifierExpression))
                            {
                                if (TryConvertQueryExpression(source, (ExpressionSyntax expression) => insertMethod(identifierExpression, expression), out var qe1))
                                {
                                    var parentExpressionStatement = FindParentNode(source, SyntaxKind.LocalDeclarationStatement); // TODO is FindParentNode the right approach? or expressionstatement?
                                    var updatedParentExpressionStatement = parentExpressionStatement.ReplaceNode(
                                        source.Parent.Parent.Parent,
                                        initializer);

                                    documentUpdate = new DocumentUpdate(parentExpressionStatement, new[] { updatedParentExpressionStatement, qe1 });
                                    return true;
                                }
                            }

                            break;
                        case SyntaxKind.ReturnStatement:
                            string freeVariableName = GetFreeSymbolName(variableName, source.GetLocation().SourceSpan.Start);
                            var identifierName = SyntaxFactory.IdentifierName(freeVariableName);
                            if (TryConvertQueryExpression(source, (ExpressionSyntax expression) => insertMethod(identifierName, expression), out var qe2))
                            {
                                var parentExpressionStatement = FindParentNode(source, SyntaxKind.ReturnStatement); // TODO is FindParentNode the right approach? or expressionstatement?
                                var variableDeclaration =
                                    SyntaxFactory.LocalDeclarationStatement(
                                        SyntaxFactory.VariableDeclaration(
                                            SyntaxFactory.IdentifierName("var"),
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.VariableDeclarator(
                                                    identifierName.Identifier,
                                                    null,
                                                    SyntaxFactory.EqualsValueClause(initializer)))));
                                var returnList = SyntaxFactory.ReturnStatement(identifierName);
                                documentUpdate = new DocumentUpdate(parentExpressionStatement, new[] { variableDeclaration, qe2, returnList });
                                return true;
                            }

                            break;
                    }
                }

                throw new NotImplementedException();
            }

            private bool TryGetVariableName(SyntaxNode node, out ExpressionSyntax identifier)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.EqualsValueClause:
                        identifier = SyntaxFactory.IdentifierName(((VariableDeclaratorSyntax)node.Parent).Identifier);
                        return true;
                    case SyntaxKind.MultiplyAssignmentExpression:
                    case SyntaxKind.SimpleAssignmentExpression:
                        identifier = ((AssignmentExpressionSyntax)node).Left;
                        return true;
                }

                identifier = null;
                return false;
            }

            private bool TryReplaceWithLocalFunction(QueryExpressionSyntax source, SyntaxKind parentNodeSyntaxKind, out DocumentUpdate documentUpdate)
            {
                SyntaxNode parentExpressionStatement = FindParentNode(source, parentNodeSyntaxKind);
                string localFunctionName = GetFreeSymbolName("localFunction", source.GetLocation().SourceSpan.Start);

                StatementSyntax[] internalNodeMethod(ExpressionSyntax expression) => new[] { SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, expression) };
                if (TryConvertQueryExpression(source, internalNodeMethod, out var qe))
                {
                    BlockSyntax body = SyntaxFactory.Block(qe);
                    if (TryGetTypeSyntax(source, out var typeSyntax))
                    {
                        var localFunctionDeclaration = SyntaxFactory.LocalFunctionStatement(
                            modifiers: default,
                            returnType: typeSyntax,
                            identifier: SyntaxFactory.Identifier(localFunctionName),
                            typeParameterList: null,
                            parameterList: SyntaxFactory.ParameterList(),
                            constraintClauses: default,
                            body: body,
                            expressionBody: null);

                        var localFunctionInvocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(localFunctionName));
                        SyntaxNode newParentExpressionStatement = parentExpressionStatement.ReplaceNode(source, localFunctionInvocation);
                        documentUpdate = new DocumentUpdate(parentExpressionStatement, new[] { localFunctionDeclaration, newParentExpressionStatement });
                        return true;
                    }
                }

                documentUpdate = null;
                return false;
            }

            private bool TryGetTypeSyntax(ExpressionSyntax expression, out TypeSyntax typeSyntax)
            {
                var typeInfo = _semanticModel.GetTypeInfo(expression, _cancellationToken);
                return TryGetTypeSyntax(typeInfo.ConvertedType, out typeSyntax);
            }

            private bool TryGetTypeSyntax(ITypeSymbol typeSymbol, out TypeSyntax typeSyntax)
            {
                typeSyntax = null;
                if (typeSymbol.IsAnonymousType)
                {
                    return false;
                }

                var namedTypeSymbol = (INamedTypeSymbol)typeSymbol;
                if (namedTypeSymbol.TypeArguments.Any())
                {
                    var list = new List<TypeSyntax>();
                    foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                    {
                        if (TryGetTypeSyntax(typeArgument, out var childTypeSyntax))
                        {
                            list.Add(childTypeSyntax);
                        }
                        else
                        {
                            return false;
                        }
                    }

                    typeSyntax = SyntaxFactory.GenericName(
                        // TODO add type formatter to convert Int32 to int
                        SyntaxFactory.Identifier(namedTypeSymbol.Name),
                        SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(list)));
                }
                else
                {
                    // TODO add type formatter to convert Int32 to int
                    typeSyntax = SyntaxFactory.IdentifierName(namedTypeSymbol.Name);
                }

                return true;
            }

            private static SyntaxNode FindParentNode(SyntaxNode node, SyntaxKind kind)
            {
                while (node != null)
                {
                    if (node.Kind() == kind)
                    {
                        return node;
                    }

                    node = node.Parent;
                }

                throw new ArgumentException($"Parent node of {kind.ToString()} not found.");
            }

            private string GetFreeSymbolName(string prefix, int position)
            {
                var symbols = _semanticModel.LookupSymbols(position);
                if (!symbols.Any(s => s.Name == prefix))
                {
                    return prefix;
                }

                int counter = 1;
                do
                {
                    string candidate = $"{prefix}{counter}";
                    if (!symbols.Any(s => s.Name == candidate))
                    {
                        return candidate;
                    }
                    counter++;
                } while (true);
            }

            private bool TryConvertInForEach(QueryExpressionSyntax source, out DocumentUpdate documentUpdate)
            {
                ForEachStatementSyntax commonForEachStatement = (ForEachStatementSyntax)source.Parent;
                ImmutableArray<StatementSyntax> statements = GetStatements(commonForEachStatement.Statement);

                StatementSyntax[] internalNodeMethod(ExpressionSyntax expression)
                {
                    if (expression.Kind() == SyntaxKind.IdentifierName && ((IdentifierNameSyntax)expression).Identifier.ValueText == commonForEachStatement.Identifier.ValueText)
                    {
                        return statements.ToArray();
                    }
                    else
                    {
                        var declaration = SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                SyntaxFactory.IdentifierName("var"),
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(commonForEachStatement.Identifier, null, SyntaxFactory.EqualsValueClause(expression)))));
                        // TODO refactor
                        var list = statements.ToList();
                        list.Insert(0, declaration);
                        return list.ToArray();
                    }
                }

                if (TryConvertQueryExpression(source, internalNodeMethod, out StatementSyntax newNode))
                {
                    documentUpdate = new DocumentUpdate(commonForEachStatement, newNode);
                    return true;
                }

                documentUpdate = null;
                return false;
            }

            private ImmutableArray<StatementSyntax> GetStatements(StatementSyntax statement)
            {
                switch (statement.Kind())
                {
                    case SyntaxKind.Block:
                        var block = (BlockSyntax)statement;
                        return ImmutableArray.CreateRange(block.Statements);
                    case SyntaxKind.ExpressionStatement:
                        return ImmutableArray.Create(statement);
                    default:
                        throw new ArgumentException(statement.Kind().ToString());
                }
            }

            private bool TryConvertInReturnStatement(QueryExpressionSyntax source, out DocumentUpdate documentUpdate)
            {
                SyntaxNode nodeToReplace = source.Parent;
                StatementSyntax[] internalNodeMethod(ExpressionSyntax expression) => new[] { SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, expression) };
                if (TryConvertQueryExpression(source, internalNodeMethod, out var newNode))
                {
                    documentUpdate = new DocumentUpdate(nodeToReplace, newNode);
                    return true;
                }

                documentUpdate = null;
                return false;
            }

            private bool TryConvertQueryExpression(QueryExpressionSyntax source, Func<ExpressionSyntax, IEnumerable<StatementSyntax>> insertMethod, out StatementSyntax statement)
            {
                FromClauseSyntax fromClause = source.FromClause;
                if (TryProcessQueryBody(insertMethod, source.Body, out var querybody))
                {
                    statement = SyntaxFactory.ForEachStatement(fromClause.Type, fromClause.Identifier, fromClause.Expression.WithoutTrivia(), querybody);
                    return true;
                }

                statement = null;
                return false;
            }

            private bool TryProcessQueryBody(Func<ExpressionSyntax, IEnumerable<StatementSyntax>> insertMethod, QueryBodySyntax queryBody, out StatementSyntax statement)
            {
                StatementSyntax internalStatement;
                if (queryBody.Continuation != null)
                {
                    // TODO use queryBody.Continuation.Identifier
                    TryProcessQueryBody(insertMethod, queryBody.Continuation.Body, out internalStatement);
                }

                var selectOrGroupClause = queryBody.SelectOrGroup;
                switch (selectOrGroupClause.Kind())
                {
                    case SyntaxKind.SelectClause:
                        // TODO must be different between the latest one and intermediate ones
                        internalStatement = SyntaxFactory.Block(insertMethod(((SelectClauseSyntax)selectOrGroupClause).Expression));
                        break;
                    case SyntaxKind.GroupClause:
                        throw new NotImplementedException();
                    case SyntaxKind.LetClause:
                    // TODO: https://github.com/dotnet/roslyn/issues/25112
                    default:
                        throw new ArgumentException(selectOrGroupClause.Kind().ToString());
                }

                foreach (QueryClauseSyntax queryClause in queryBody.Clauses.Reverse())
                {
                    switch (queryClause.Kind())
                    {
                        case SyntaxKind.WhereClause:
                            WhereClauseSyntax whereClause = (WhereClauseSyntax)queryClause;
                            internalStatement = SyntaxFactory.Block(SyntaxFactory.IfStatement(whereClause.Condition, internalStatement));
                            break;
                        case SyntaxKind.FromClause:
                            FromClauseSyntax fromClause = (FromClauseSyntax)queryClause;
                            internalStatement = SyntaxFactory.Block(SyntaxFactory.ForEachStatement(fromClause.Type, fromClause.Identifier, fromClause.Expression.WithoutTrivia(), internalStatement));
                            break;
                        case SyntaxKind.JoinClause: // TODO: https://github.com/dotnet/roslyn/issues/25112
                        case SyntaxKind.OrderByClause:// OrderBy is not supported by foreach.
                        default:
                            statement = null;
                            return false;

                    }
                }

                statement = internalStatement;
                return true;
            }
        }
    }
}
