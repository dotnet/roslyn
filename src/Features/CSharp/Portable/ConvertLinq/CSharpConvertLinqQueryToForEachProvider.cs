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
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq
{
    internal sealed class CSharpConvertLinqQueryToLinqMethodProvider : AbstractConvertLinqQueryToLinqMethodProvider<QueryExpressionSyntax>
    {
        private static readonly TypeSyntax VarNameIdentifier = SyntaxFactory.IdentifierName("var");

        protected override string Title => CSharpFeaturesResources.Convert_to_foreach;

        protected override bool TryConvert(
            QueryExpressionSyntax queryExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out DocumentUpdateInfo documentUpdateInfo)
            => Converter.TryConvert(queryExpression, semanticModel, cancellationToken, out documentUpdateInfo);

        private sealed class Converter
        {
            private readonly SemanticModel _semanticModel;
            private readonly CancellationToken _cancellationToken;

            private Converter(SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _cancellationToken = cancellationToken;
            }

            public static bool TryConvert(
                QueryExpressionSyntax queryExpression,
                SemanticModel semanticModel,
                CancellationToken cancellationToken,
                out DocumentUpdateInfo documentUpdateInfo)
                => new Converter(semanticModel, cancellationToken).TryConvert(queryExpression, out documentUpdateInfo);

            private bool TryConvert(QueryExpressionSyntax source, out DocumentUpdateInfo documentUpdateInfo)
            {
                if (source.DescendantTrivia().Any(trivia => trivia.MatchesKind(
                    SyntaxKind.SingleLineCommentTrivia,
                    SyntaxKind.MultiLineCommentTrivia,
                    SyntaxKind.MultiLineDocumentationCommentTrivia) ||
                    source.ContainsDirectives))
                {
                    documentUpdateInfo = default;
                    return false;
                }

                // GetDiagnostics is expensive. Move it to the end if there were no bail outs from the algorithm.
                // TODO likely adding more semantic checks will perform checks we expect from GetDiagnostics
                // We may consider removing GetDiagnostics.
                // https://github.com/dotnet/roslyn/issues/25639
                if (!TryConvertInternal(source, out documentUpdateInfo) || 
                    _semanticModel.GetDiagnostics(source.Span, _cancellationToken).Any())
                {
                    documentUpdateInfo = default;
                    return false;
                }

                return true;
            }

            private bool TryConvertInternal(QueryExpressionSyntax source, out DocumentUpdateInfo documentUpdateInfo)
            {
                // (from a in b select a); 
                SyntaxNode parent = source.WalkUpParentheses().Parent;

                // (from a in b select a)?.ToList();
                if (parent.Kind() == SyntaxKind.ConditionalAccessExpression)
                {
                    parent = parent.Parent;
                }

                switch (parent.Kind())
                {
                    case SyntaxKind.ReturnStatement:
                        // return from a in b select a;
                        return TryConvertIfInReturnStatement(source, (ReturnStatementSyntax)parent, out documentUpdateInfo);
                    case SyntaxKind.ForEachStatement:
                        // foreach(var x in from a in b select a)
                        return TryConvertIfInForEach(source, (ForEachStatementSyntax)parent, out documentUpdateInfo);
                    case SyntaxKind.EqualsValueClause:
                        // var a = from a in b select a
                    case SyntaxKind.SimpleAssignmentExpression:
                        // a = from a in b select a
                        return TryReplaceWithLocalFunction(source, parent, out documentUpdateInfo);
                    case SyntaxKind.SimpleMemberAccessExpression:
                        // (from a in b select a).ToList(), (from a in b select a).Count(), etc.
                        return TryConvertIfInMemberAccessExpression(source, (MemberAccessExpressionSyntax)parent, out documentUpdateInfo);
                    default:
                        documentUpdateInfo = default;
                        return false;
                }
            }

            private bool TryConvertIfInMemberAccessExpression(
               QueryExpressionSyntax source,
               MemberAccessExpressionSyntax memberAccessExpression,
               out DocumentUpdateInfo documentUpdateInfo)
            {
                if (memberAccessExpression.Parent is InvocationExpressionSyntax invocationExpression)
                {
                    // This also covers generic names (i.e. with type arguments) like 'ToList<int>'. 
                    // The ValueText is still just 'ToList'. 
                    switch (memberAccessExpression.Name.Identifier.ValueText)
                    {
                        case nameof(Enumerable.ToList):
                            return TryConvertIfInToListInvocation(source, invocationExpression, out documentUpdateInfo);
                        case nameof(Enumerable.Count):
                            return TryConvertIfInCountInvocation(source, invocationExpression, out documentUpdateInfo);
                        default:
                            return TryReplaceWithLocalFunction(source, memberAccessExpression, out documentUpdateInfo);
                    }
                }

                documentUpdateInfo = default;
                return false;
            }

            private bool TryConvertIfInCountInvocation(
                QueryExpressionSyntax source,
                InvocationExpressionSyntax invocationExpression,
                out DocumentUpdateInfo documentUpdateInfo)
            {
                if (_semanticModel.GetSymbolInfo(invocationExpression, _cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.Parameters.Length == 0 && methodSymbol.ReturnType?.SpecialType == SpecialType.System_Int32)
                {
                    // before var count = (from a in b select a).Count();
                    // after
                    // var count = 0;
                    // foreach (var a in b)
                    // {
                    //     count++;
                    //  }
                    return TryConvertIfInInvocation(
                            source,
                            invocationExpression,
                            (variableIdentifier, expression) => SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, variableIdentifier)), // Generating 'count++'
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)), // count = 0
                            variableName: "count",
                            out documentUpdateInfo);
                }

                documentUpdateInfo = default;
                return false;
            }

            private bool TryConvertIfInToListInvocation(
                QueryExpressionSyntax source,
                InvocationExpressionSyntax invocationExpression,
                out DocumentUpdateInfo documentUpdateInfo)
            {
                // before var list = (from a in b select a).ToList();
                // after
                // var list = new List<T>();
                // foreach (var a in b)
                // {
                //     list.Add(a)
                //  }
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
                                  initializer: null),
                               variableName: "list",
                              out documentUpdateInfo);
                }

                documentUpdateInfo = default;
                return false;
            }

            private bool TryConvertIfInInvocation(
                QueryExpressionSyntax source,
                InvocationExpressionSyntax invocationExpression,
                Func<ExpressionSyntax, ExpressionSyntax, StatementSyntax> leafExpressionCreationMethod,
                ExpressionSyntax initializer,
                string variableName,
                out DocumentUpdateInfo documentUpdateInfo)
            {
                var parentStatement = invocationExpression.GetAncestorOrThis<StatementSyntax>();
                if (parentStatement != null)
                {
                    if (TryConvertIfInInvocationInternal(
                            source,
                            invocationExpression,
                            parentStatement,
                            initializer,
                            variableName,
                            out var variable,
                            out var documentCreationMethod))
                    {
                        if (TryConvertQueryExpression(
                            source,
                            expression => leafExpressionCreationMethod(variable, expression),
                            out var statement))
                        {
                            documentUpdateInfo = documentCreationMethod(statement);
                            return true;
                        }
                    }
                }

                documentUpdateInfo = default;
                return false;
            }

            private bool TryConvertIfInInvocationInternal(
                QueryExpressionSyntax source,
                InvocationExpressionSyntax invocationExpression,
                StatementSyntax parentStatement,
                ExpressionSyntax initializer,
                string variableName,
                out ExpressionSyntax variable,
                out Func<StatementSyntax, DocumentUpdateInfo> documentUpdateMethod)
            {
                var invocationParent = invocationExpression.Parent;
                switch (invocationParent.Kind())
                {
                    case SyntaxKind.EqualsValueClause:
                        if (invocationParent.Parent is VariableDeclaratorSyntax variableDeclarator)
                        {
                            // before var a = (from a in b select a).ToList();
                            // after var a = new List<T>();
                            // foreach(...)
                            variable = SyntaxFactory.IdentifierName(variableDeclarator.Identifier);
                            documentUpdateMethod =
                                s => new DocumentUpdateInfo(
                                    parentStatement,
                                    new[] { parentStatement.ReplaceNode(invocationExpression, initializer), s });
                            return true;
                        }
                        break;
                    case SyntaxKind.SimpleAssignmentExpression:
                        // before a = (from a in b select a).ToList();
                        // after a = new List<T>();
                        // foreach(...)
                        variable = ((AssignmentExpressionSyntax)invocationParent).Left;
                        documentUpdateMethod = s =>
                            new DocumentUpdateInfo(
                                parentStatement,
                                new[] { parentStatement.ReplaceNode(invocationExpression, initializer), s });
                        return true;
                    case SyntaxKind.ReturnStatement:
                        // before return (from a in b select a).ToList();
                        // after var list = new List<T>();
                        // foreach(...)
                        // return list;
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
                        documentUpdateMethod = s => new DocumentUpdateInfo(
                            parentStatement,
                            new[] { variableDeclaration, s, SyntaxFactory.ReturnStatement(identifierName) });
                        return true;
                        // SyntaxKind.ArrowExpressionClause is not supported
                }

                documentUpdateMethod = default;
                variable = default;
                return false;
            }

            private bool TryReplaceWithLocalFunction(QueryExpressionSyntax source, SyntaxNode parent, out DocumentUpdateInfo documentUpdateInfo)
            {
                var parentStatement = parent.GetAncestorOrThis<StatementSyntax>();
                if (parentStatement != null)
                {
                    StatementSyntax internalNodeMethod(ExpressionSyntax expression)
                        => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, expression);

                    if (TryConvertQueryExpression(source, internalNodeMethod, out var convertedFromQueryExpression))
                    {
                        if (TryGetTypeSyntax(source, out var typeSyntax))
                        {
                            // before statement ... from a in select b ...
                            // after
                            // IEnumerable<T> localFunction()
                            // {
                            //   foreach(var a in b)
                            //   {
                            //       yield return a;
                            //   }
                            //  }
                            //  statement ... localFunction();
                            string localFunctionName = GetFreeSymbolName("localFunction", source.GetLocation().SourceSpan.Start);
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
                            documentUpdateInfo = new DocumentUpdateInfo(parentStatement, new[] { localFunctionDeclaration, newParentExpressionStatement });
                            return true;
                        }
                    }
                }

                documentUpdateInfo = default;
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

            private bool TryConvertIfInForEach(QueryExpressionSyntax source, ForEachStatementSyntax forEachStatement, out DocumentUpdateInfo documentUpdateInfo)
            {
                // before foreach(var x in from a in b select a)
                var statement = forEachStatement.Statement;
                StatementSyntax internalNodeMethod(ExpressionSyntax expression)
                {
                    // before foreach(var a in from ... a) { dosomething(a); }
                    // after 
                    // foreach (var a in ...)
                    // ...
                    //{ dosomething(a); }
                    if (expression is IdentifierNameSyntax identifier &&
                        identifier.Identifier.ValueText == forEachStatement.Identifier.ValueText)
                    {
                        return statement;
                    }
                    else
                    {
                        // before foreach(var x in from ... a) { dosomething(x); }
                        // after 
                        // foreach (var a in ...)
                        // ...
                        // {
                        //      var x = a;
                        //      dosomething(x); 
                        //  }
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
                    documentUpdateInfo = new DocumentUpdateInfo(forEachStatement, newNode);
                    return true;
                }

                documentUpdateInfo = default;
                return false;
            }

            private bool TryConvertIfInReturnStatement(QueryExpressionSyntax source, ReturnStatementSyntax returnStatement, out DocumentUpdateInfo documentUpdateInfo)
            {
                // before: return from a in b select a;
                // after: 
                // foreach(var a in b)
                // {
                //      yield return a;
                // }
                StatementSyntax internalNodeMethod(ExpressionSyntax expression)
                    => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, expression);

                if (TryConvertQueryExpression(source, internalNodeMethod, out var newNode))
                {
                    documentUpdateInfo = new DocumentUpdateInfo(returnStatement, newNode);
                    return true;
                }

                documentUpdateInfo = default;
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
                if (IsValidWhereClauseSymbol(whereClause))
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

            private bool IsValidWhereClauseSymbol(WhereClauseSyntax whereClause)
            {
                if (!TryGetMethodSymbol(whereClause, out var methodSymbol))
                {
                    return false;
                }

                var returnType = methodSymbol.ReturnType;
                if (returnType.Name != "IEnumerable") // TODO better that name check?
                {
                    return false;
                }

                var typeArguments = returnType.GetTypeArguments();
                if (typeArguments.Length != 1)
                {
                    return false;
                }

                var typeArgument = typeArguments.Single();
                switch (methodSymbol.Parameters.Length)
                {
                    // IEnumerable<T> Where(Func<T, bool> predicate) 
                    case 1:
                        return IsPredicate(methodSymbol.Parameters.First().Type, typeArgument);

                    // IEnumerable<T> Where(this IEnumerable<T>, Func<T, bool> predicate) 
                    case 2:
                        // TODO add checks for this.
                        return IsPredicate(methodSymbol.Parameters.First().Type, typeArgument);

                    default: return false;
                }
            }

            private bool TryGetMethodSymbol(QueryClauseSyntax queryClause, out IMethodSymbol methodSymbol)
            {
                QueryClauseInfo queryClauseInfo = _semanticModel.GetQueryClauseInfo(queryClause, _cancellationToken);
                methodSymbol = queryClauseInfo.OperationInfo.Symbol as IMethodSymbol;
                return methodSymbol != null;
            }

            private static bool IsPredicate(ITypeSymbol typeSymbol, ITypeSymbol typeArgument)
            {
                if (typeSymbol.Name != "Func")  // TODO better that name check?
                {
                    return false;
                }

                var typeArguments = typeSymbol.GetTypeArguments();
                if (typeArguments.Length != 2)
                {
                    return false;
                }

                return typeArguments.Last()?.SpecialType == SpecialType.System_Boolean &&
                    SignatureComparer.Instance.HaveSameSignature(typeArguments.First(), typeArgument, caseSensitive: true);
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
                                                argumentList: null,
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
                                                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                                                    SyntaxFactory.IdentifierName(nameof(object.Equals))),
                                                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                                                    new[] {
                                                        SyntaxFactory.Argument(joinClause.LeftExpression),
                                                        SyntaxFactory.Argument(joinClause.RightExpression.WithoutTrailingTrivia())}))),
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
