// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq
{
    internal sealed class CSharpConvertLinqQueryToLinqMethodProvider : AbstractConvertLinqProvider
    {
        private static readonly TypeSyntax VarNameIdentifier = SyntaxFactory.IdentifierName("var");

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
                if (_semanticModel.GetDiagnostics(source.Span, _cancellationToken).Any())
                {
                    documentUpdate = default;
                    return false;
                }

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
                        documentUpdate = default;
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
                                // TODO is this correct? move inside TryGetTypeSyntax?
                                if (typeSyntax.Kind() == SyntaxKind.QualifiedName)
                                {
                                    typeSyntax = ((QualifiedNameSyntax)typeSyntax).Right;
                                }

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
                                        ((GenericNameSyntax)typeSyntax).TypeArgumentList).WithAdditionalAnnotations(Simplifier.Annotation), // / TODO "List"?
                                    SyntaxFactory.ArgumentList(), null);
                                variableNamePrefix = "list";
                            }
                            else
                            {
                                documentUpdate = default;
                                return false; // refactor
                            }
                            break;
                        case nameof(Enumerable.Count):
                            // TODO exclude or support Count(predicate)
                            variableNamePrefix = "count";
                            insideLoopExpression = (ExpressionSyntax variableIdentifier, ExpressionSyntax expression) =>
                                    SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, variableIdentifier);
                            initializer = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0));
                            break;
                        default:
                            // TODO there can be also not a declaration but a return or an assignement - supported or a proerty - not supported.
                            return TryReplaceWithLocalFunction(source, SyntaxKind.LocalDeclarationStatement, out documentUpdate);
                    }

                    return TryConvertInInvocation(
                              source,
                             (ExpressionSyntax variableIdentifier, ExpressionSyntax expression) => SyntaxFactory.ExpressionStatement(insideLoopExpression(variableIdentifier, expression)),
                              initializer,
                              variableNamePrefix,
                              out documentUpdate);
                }

                documentUpdate = default;
                return false;
            }

            private bool TryConvertInInvocation(
                QueryExpressionSyntax source,
                Func<ExpressionSyntax, ExpressionSyntax, StatementSyntax> insertMethod,
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
                                            VarNameIdentifier,
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

                        default:
                            throw new NotImplementedException(parenthesizedExpression.Parent.Parent.Parent.Kind().ToString());
                    }
                }

                throw new NotImplementedException(parenthesizedExpression.Parent.Parent.Kind().ToString());
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

                StatementSyntax internalNodeMethod(ExpressionSyntax expression) => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, expression);
                if (TryConvertQueryExpression(source, internalNodeMethod, out var qe))
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
                                SyntaxFactory.SingletonList(qe),
                                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)),
                            expressionBody: null);

                        var localFunctionInvocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(localFunctionName));
                        SyntaxNode newParentExpressionStatement = parentExpressionStatement.ReplaceNode(source, localFunctionInvocation);
                        documentUpdate = new DocumentUpdate(parentExpressionStatement, new[] { localFunctionDeclaration, newParentExpressionStatement });
                        return true;
                    }
                }

                documentUpdate = default;
                return false;
            }

            private bool TryGetTypeSyntax(ExpressionSyntax expression, out TypeSyntax typeSyntax)
            {
                var typeInfo = _semanticModel.GetTypeInfo(expression, _cancellationToken);
                if (typeInfo.Type.TypeKind == TypeKind.Error)
                {
                    typeSyntax = default;
                    return false;
                }

                return TryGetTypeSyntax(typeInfo.Type, out typeSyntax);
            }

            private bool TryGetTypeSyntax(ITypeSymbol typeSymbol, out TypeSyntax typeSyntax)
            {
                typeSyntax = null;
                if (typeSymbol.IsAnonymousType)
                {
                    return false;
                }

                typeSyntax = typeSymbol.GenerateTypeSyntax();
                return true;

                //var namedTypeSymbol = (INamedTypeSymbol)typeSymbol;
                //if (namedTypeSymbol.TypeArguments.Any())
                //{
                //    var list = new List<TypeSyntax>();
                //    foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                //    {
                //        if (TryGetTypeSyntax(typeArgument, out var childTypeSyntax))
                //        {
                //            list.Add(childTypeSyntax);
                //        }
                //        else
                //        {
                //            return false;
                //        }
                //    }

                //    typeSyntax = SyntaxFactory.GenericName(
                //        // TODO add type formatter to convert Int32 to int
                //        SyntaxFactory.Identifier(namedTypeSymbol.Name),
                //        SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(list))).WithAdditionalAnnotations(Simplifier.Annotation);
                //}
                //else
                //{
                //    // TODO add type formatter to convert Int32 to int
                //    typeSyntax = SyntaxFactory.IdentifierName(namedTypeSymbol.Name).WithAdditionalAnnotations(Simplifier.Annotation);
                //}

                //return true;
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
                StatementSyntax statement = commonForEachStatement.Statement;

                StatementSyntax internalNodeMethod(ExpressionSyntax expression)
                {
                    if (expression.Kind() == SyntaxKind.IdentifierName && ((IdentifierNameSyntax)expression).Identifier.ValueText == commonForEachStatement.Identifier.ValueText)
                    {
                        return statement;
                    }
                    else
                    {
                        var declaration = SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                VarNameIdentifier,
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(commonForEachStatement.Identifier, null, SyntaxFactory.EqualsValueClause(expression)))));
                        // TODO refactor
                        return AddToBlockTop(declaration, statement);
                    }
                }

                if (TryConvertQueryExpression(source, internalNodeMethod, out StatementSyntax newNode))
                {
                    documentUpdate = new DocumentUpdate(commonForEachStatement, newNode);
                    return true;
                }

                documentUpdate = default;
                return false;
            }

            private bool TryConvertInReturnStatement(QueryExpressionSyntax source, out DocumentUpdate documentUpdate)
            {
                SyntaxNode nodeToReplace = source.Parent;
                StatementSyntax internalNodeMethod(ExpressionSyntax expression) => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, expression);
                if (TryConvertQueryExpression(source, internalNodeMethod, out var newNode))
                {
                    documentUpdate = new DocumentUpdate(nodeToReplace, newNode);
                    return true;
                }

                documentUpdate = default;
                return false;
            }

            private bool TryConvertQueryExpression(QueryExpressionSyntax source, Func<ExpressionSyntax, StatementSyntax> insertMethod, out StatementSyntax statement)
            {
                FromClauseSyntax fromClause = source.FromClause;
                ProcessingState processingState = new ProcessingState(
                    fromClause.Identifier.ValueText,
                    s => SyntaxFactory.ForEachStatement(fromClause.Type ?? VarNameIdentifier, fromClause.Identifier, fromClause.Expression.WithoutTrivia(), s));

                if (TryProcessQueryBody(insertMethod, source.Body, processingState))
                {
                    statement = processingState.ExecuteMethods();
                    return true;
                }

                statement = default;
                return false;
            }

            // TODO maybe there should be two traverses: first to send variables to the next level and the back to create expressions.
            private bool TryProcessQueryBody(Func<ExpressionSyntax, StatementSyntax> insertMethod, QueryBodySyntax queryBody, ProcessingState processingState)
            {
                foreach (QueryClauseSyntax queryClause in queryBody.Clauses)
                {
                    switch (queryClause.Kind())
                    {
                        case SyntaxKind.WhereClause:
                            processingState.AddStatementProcessingMethod(s =>
                            {
                                WhereClauseSyntax whereClause = (WhereClauseSyntax)queryClause;
                                return SyntaxFactory.Block(SyntaxFactory.IfStatement(ProcessCondition(whereClause.Condition), s));
                            });
                            break;
                        case SyntaxKind.LetClause:
                            LetClauseSyntax letClause = (LetClauseSyntax)queryClause;
                            processingState.AddVariableName(letClause.Identifier.ValueText);
                            processingState.AddStatementProcessingMethod(
                                s =>
                                {
                                    LocalDeclarationStatementSyntax localDeclarationStatement =
                                       SyntaxFactory.LocalDeclarationStatement(
                                           SyntaxFactory.VariableDeclaration(
                                               VarNameIdentifier,
                                               SyntaxFactory.SingletonSeparatedList(
                                                   SyntaxFactory.VariableDeclarator(
                                                       letClause.Identifier,
                                                       null,
                                                       SyntaxFactory.EqualsValueClause(letClause.Expression)))));
                                    // TODO may miss blocks if there are some lets on after another
                                    // TODO unit test with Let / Let / Let
                                    return AddToBlockTop(localDeclarationStatement, s);
                                });
                            break;
                        case SyntaxKind.FromClause:
                            FromClauseSyntax fromClause = (FromClauseSyntax)queryClause;
                            processingState.AddVariableName(fromClause.Identifier.ValueText);
                            processingState.AddStatementProcessingMethod(
                                s =>
                                {
                                    return SyntaxFactory.Block(
                                        SyntaxFactory.ForEachStatement(
                                            fromClause.Type ?? VarNameIdentifier,
                                            fromClause.Identifier,
                                            fromClause.Expression.WithoutTrivia(), s));
                                });
                            break;
                        case SyntaxKind.JoinClause:
                            JoinClauseSyntax joinClause = (JoinClauseSyntax)queryClause;

                            if (joinClause.Into != null)
                            {
                                processingState.AddVariableName(joinClause.Identifier.ValueText);
                                processingState.AddStatementProcessingMethod(
                                    s =>
                                    {
                                        var intoDeclaration =
                                        SyntaxFactory.LocalDeclarationStatement(
                                            SyntaxFactory.VariableDeclaration(
                                                VarNameIdentifier,
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.VariableDeclarator(
                                                        joinClause.Into.Identifier,
                                                        null,
                                                        SyntaxFactory.EqualsValueClause(
                                                            SyntaxFactory.AnonymousObjectCreationExpression(
                                                                SyntaxFactory.SeparatedList(
                                                                   processingState.VariableNames.Select(vn => CreateAnonymousObjectMemberDeclarator(vn)))))))));
                                        return CreateJoinStatement(joinClause, AddToBlockTop(intoDeclaration, s));
                                    });
                            }
                            else
                            {
                                processingState.AddVariableName(joinClause.Identifier.ValueText);
                                processingState.AddStatementProcessingMethod(s => CreateJoinStatement(joinClause, s));
                            }

                            break;
                        case SyntaxKind.OrderByClause:// OrderBy is not supported by foreach.
                        default:
                            return false;
                    }
                }

                var selectOrGroupClause = queryBody.SelectOrGroup;
                switch (selectOrGroupClause.Kind())
                {
                    case SyntaxKind.SelectClause:
                        // TODO must be different between the latest one and intermediate ones
                        // TODO add a unit test with multiple selects
                        // TODO select into
                        var selectClause = (SelectClauseSyntax)selectOrGroupClause;

                        // End of the queue
                        if (queryBody.Continuation == null)
                        {
                            processingState.AddStatementProcessingMethod(s => WrapWithBlock(insertMethod(((SelectClauseSyntax)selectOrGroupClause).Expression)));
                        }
                        else
                        {
                            // TODO check with unit tests
                            processingState.AddVariableName(queryBody.Continuation.Identifier.ValueText);
                            processingState.AddStatementProcessingMethod(
                                s => AddToBlockTop(
                                     SyntaxFactory.LocalDeclarationStatement(
                                        SyntaxFactory.VariableDeclaration(
                                            VarNameIdentifier,
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.VariableDeclarator(
                                                    queryBody.Continuation.Identifier,
                                                    null,
                                                    SyntaxFactory.EqualsValueClause(selectClause.Expression))))), s));

                            // TODO use queryBody.Continuation.Identifier
                            // TODO or should we update variable names first?
                            TryProcessQueryBody(insertMethod, queryBody.Continuation.Body, processingState);
                        }
                        break;
                    case SyntaxKind.GroupClause:
                        // Groupclause is not supported by the conversion
                        return false;
                    default:
                        throw new ArgumentException(selectOrGroupClause.Kind().ToString());
                }

                return true;
            }

            private static BlockSyntax AddToBlockTop(StatementSyntax newStatement, StatementSyntax statement)
            {
                if (statement.Kind() == SyntaxKind.Block)
                {
                    var list = new List<StatementSyntax>();
                    list.Add(newStatement);
                    list.AddRange(((BlockSyntax)statement).Statements); // TODO refactor
                    return SyntaxFactory.Block(list);
                }
                else
                {
                    return SyntaxFactory.Block(newStatement, statement);
                }
            }

            private static BlockSyntax WrapWithBlock(StatementSyntax statement)
            {
                if (statement.Kind() == SyntaxKind.Block)
                {
                    return (BlockSyntax)statement;
                }
                else
                {
                    return SyntaxFactory.Block(statement);
                }
            }

            private static ExpressionSyntax ProcessCondition(ExpressionSyntax expression)
            {
                if (expression.Kind() == SyntaxKind.ParenthesizedExpression)
                {
                    expression = ((ParenthesizedExpressionSyntax)expression).Expression;
                }

                return expression.WithoutTrivia();
            }

            private static AnonymousObjectMemberDeclaratorSyntax CreateAnonymousObjectMemberDeclarator(string name)
            {
                return SyntaxFactory.AnonymousObjectMemberDeclarator(SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(name)), SyntaxFactory.IdentifierName(name));
            }

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
                                                    SyntaxFactory.IdentifierName("Equals")), // TODO get name from nameof
                                                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(joinClause.RightExpression.WithoutTrailingTrivia())))),
                                            statement))));
            }


            private class ProcessingState
            {
                public ImmutableArray<string> VariableNames { get; private set; }
                public Stack<Func<StatementSyntax, StatementSyntax>> StatementProcessingMethods { get; }

                public ProcessingState(string variableName, Func<StatementSyntax, StatementSyntax> statementProcessingMethod)
                {
                    VariableNames = ImmutableArray.Create(variableName);
                    StatementProcessingMethods = new Stack<Func<StatementSyntax, StatementSyntax>>();
                    this.AddStatementProcessingMethod(statementProcessingMethod);
                }

                public void AddVariableName(string variableName)
                {
                    VariableNames = VariableNames.Concat(variableName);
                }

                public void AddStatementProcessingMethod(Func<StatementSyntax, StatementSyntax> method)
                {
                    StatementProcessingMethods.Push(method);
                }

                public StatementSyntax ExecuteMethods()
                {
                    StatementSyntax statement = default;
                    while (StatementProcessingMethods.Any())
                    {
                        var method = StatementProcessingMethods.Pop();
                        statement = method(statement);
                    }
                    return statement;
                }
            }
        }
    }
}
