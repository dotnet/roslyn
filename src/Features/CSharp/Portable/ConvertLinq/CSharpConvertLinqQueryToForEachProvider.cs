// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq
{
    internal sealed class CSharpConvertLinqQueryToForEachProvider : AbstractConvertLinqQueryToForEachProvider<QueryExpressionSyntax>
    {
        private static readonly TypeSyntax VarNameIdentifier = SyntaxFactory.IdentifierName("var");

        protected override string Title => CSharpFeaturesResources.Convert_to_foreach;

        protected override bool TryConvert(
            QueryExpressionSyntax queryExpression,
            SemanticModel semanticModel,
            ISemanticFactsService semanticFacts,
            CancellationToken cancellationToken,
            out DocumentUpdateInfo documentUpdateInfo)
                => new Converter(semanticModel, semanticFacts, queryExpression, cancellationToken).TryConvert(out documentUpdateInfo);

        /// <summary>
        /// Finds a node for the span and checks that it is either a QueryExpressionSyntax or a QueryExpressionSyntax argument within ArgumentSyntax.
        /// </summary>
        protected override QueryExpressionSyntax FindNodeToRefactor(SyntaxNode root, TextSpan span)
        {
            var node = root.FindNode(span);
            return node as QueryExpressionSyntax ?? (node is ArgumentSyntax argument ? argument.Expression as QueryExpressionSyntax : default);
        }

        private sealed class Converter
        {
            private readonly SemanticModel _semanticModel;
            private readonly ISemanticFactsService _semanticFacts;
            private readonly CancellationToken _cancellationToken;
            private readonly QueryExpressionSyntax _source;
            private readonly List<string> _usedNames;

            public Converter(SemanticModel semanticModel, ISemanticFactsService semanticFacts, QueryExpressionSyntax source, CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _semanticFacts = semanticFacts;
                _source = source;
                _usedNames = new List<string>();
                _cancellationToken = cancellationToken;
            }

            public bool TryConvert(out DocumentUpdateInfo documentUpdateInfo)
            {
                // Do not try refactoring queries with comments of conditional compilation in them.
                // We can consider supporting queries with comments in the future.
                if (_source.DescendantTrivia().Any(trivia => trivia.MatchesKind(
                        SyntaxKind.SingleLineCommentTrivia,
                        SyntaxKind.MultiLineCommentTrivia,
                        SyntaxKind.MultiLineDocumentationCommentTrivia) ||
                    _source.ContainsDirectives))
                {
                    documentUpdateInfo = default;
                    return false;
                }

                // GetDiagnostics is expensive. Move it to the end if there were no bail outs from the algorithm.
                // TODO likely adding more semantic checks will perform checks we expect from GetDiagnostics
                // We may consider removing GetDiagnostics.
                // https://github.com/dotnet/roslyn/issues/25639
                if (!TryConvertInternal(out documentUpdateInfo) ||
                    _semanticModel.GetDiagnostics(_source.Span, _cancellationToken).Any())
                {
                    documentUpdateInfo = default;
                    return false;
                }

                return true;
            }

            private StatementSyntax ProcessClause(CSharpSyntaxNode node, StatementSyntax statement, out StatementSyntax extraStatementToAddAbove)
            {
                extraStatementToAddAbove = default;
                switch (node.Kind())
                {
                    case SyntaxKind.WhereClause:
                        return SyntaxFactory.Block(SyntaxFactory.IfStatement(((WhereClauseSyntax)node).Condition.WithAdditionalAnnotations(Simplifier.Annotation).WithoutTrivia(), statement));
                    case SyntaxKind.FromClause:
                        var fromClause = (FromClauseSyntax)node;
                        return SyntaxFactory.ForEachStatement(
                                    fromClause.Type ?? VarNameIdentifier,
                                    fromClause.Identifier,
                                    fromClause.Expression.WithoutTrivia(), WrapWithBlock(statement));
                    case SyntaxKind.LetClause:
                        var letClause = (LetClauseSyntax)node;
                        return AddToBlockTop(CreateLocalDeclarationStatement(letClause.Identifier, letClause.Expression), statement);
                    case SyntaxKind.JoinClause:
                        var joinClause = (JoinClauseSyntax)node;
                        if (joinClause.Into != null)
                        {
                            // This must be caught on the validation step. Therefore, here is an exception.
                            throw new ArgumentException("GroupJoin is not supported");
                        }
                        else
                        {
                            ExpressionSyntax expression;
                            if (IsLocalOrParameterSymbol(_semanticModel.GetOperation(joinClause.InExpression, _cancellationToken)))
                            {
                                expression = joinClause.InExpression;
                            }
                            else
                            {
                                // Input: var q = from x in XX() join y in YY() on x equals y select x + y;
                                // Add 
                                // var yy = YY();
                                string expressionName = _semanticFacts.GenerateNameForExpression(
                                    _semanticModel,
                                    joinClause.InExpression,
                                    capitalize: false,
                                    _cancellationToken);
                                SyntaxToken variable = GetFreeSymbolName(expressionName);
                                extraStatementToAddAbove = CreateLocalDeclarationStatement(variable, joinClause.InExpression);

                                // Replace YY() with yy declared above.
                                expression = SyntaxFactory.IdentifierName(variable);
                            }

                            return SyntaxFactory.Block(
                                SyntaxFactory.ForEachStatement(
                                    joinClause.Type ?? VarNameIdentifier,
                                    joinClause.Identifier,
                                    expression,
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
                                            statement)))).WithAdditionalAnnotations(Simplifier.Annotation);
                        }
                    case SyntaxKind.SelectClause:
                        // This is not the latest Select in the Query Expression
                        // There is a QueryBody with the Continuation as a parent.

                        var selectClause = (SelectClauseSyntax)node;
                        var identifier = ((QueryBodySyntax)selectClause.Parent).Continuation.Identifier;
                        return AddToBlockTop(CreateLocalDeclarationStatement(identifier, selectClause.Expression), statement);
                    default:
                        throw new ArgumentException($"Unexpected node kind {node.Kind().ToString()}");
                }
            }

            private bool TryConvertInternal(out DocumentUpdateInfo documentUpdateInfo)
            {
                // (from a in b select a); 
                SyntaxNode parent = _source.WalkUpParentheses().Parent;

                switch (parent.Kind())
                {
                    // return from a in b select a;
                    case SyntaxKind.ReturnStatement:
                        return TryConvertIfInReturnStatement((ReturnStatementSyntax)parent, out documentUpdateInfo);
                    // foreach(var x in from a in b select a)
                    case SyntaxKind.ForEachStatement:
                        return TryConvertIfInForEach((ForEachStatementSyntax)parent, out documentUpdateInfo);
                    // (from a in b select a).ToList(), (from a in b select a).Count(), etc.
                    case SyntaxKind.SimpleMemberAccessExpression:
                        return TryConvertIfInMemberAccessExpression((MemberAccessExpressionSyntax)parent, out documentUpdateInfo);
                    // var a = new [] { from a in b select a };  
                    case SyntaxKind.ArrayInitializerExpression:
                    // var a = from a in b select a
                    case SyntaxKind.EqualsValueClause:
                    // (from a in b select a)?.ToList();
                    case SyntaxKind.ConditionalAccessExpression:
                    // new new List<IEnumerable<int>> { from a in q select a * a };
                    case SyntaxKind.CollectionInitializerExpression:
                    // new new List<int>(from a in q select a * a );
                    case SyntaxKind.Argument:
                        return TryReplaceWithLocalFunction(parent, out documentUpdateInfo);
                    // a = from a in b select a
                    case SyntaxKind.SimpleAssignmentExpression:
                        // Check that query expression is on the right hand side
                        if (((AssignmentExpressionSyntax)parent).Right.WalkDownParentheses() == _source)
                        {
                            return TryReplaceWithLocalFunction(parent, out documentUpdateInfo);
                        }

                        break;
                }

                documentUpdateInfo = default;
                return false;
            }

            private bool TryConvertIfInMemberAccessExpression(
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
                            return TryConvertIfInToListInvocation(invocationExpression, out documentUpdateInfo);
                        case nameof(Enumerable.Count):
                            return TryConvertIfInCountInvocation(invocationExpression, out documentUpdateInfo);
                        default:
                            return TryReplaceWithLocalFunction(memberAccessExpression, out documentUpdateInfo);
                    }
                }

                documentUpdateInfo = default;
                return false;
            }

            private bool TryConvertIfInCountInvocation(
                InvocationExpressionSyntax invocationExpression,
                out DocumentUpdateInfo documentUpdateInfo)
            {
                if (_semanticModel.GetSymbolInfo(invocationExpression, _cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.Parameters.Length == 0 &&
                    methodSymbol.ReturnType?.SpecialType == SpecialType.System_Int32 &&
                    !methodSymbol.ReturnsByRef)
                {
                    // before var count = (from a in b select a).Count();
                    // after
                    // var count = 0;
                    // foreach (var a in b)
                    // {
                    //     count++;
                    //  }
                    return TryConvertIfInInvocation(
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
                InvocationExpressionSyntax invocationExpression,
                out DocumentUpdateInfo documentUpdateInfo)
            {
                // before var list = (from a in b select a).ToList();
                // after
                // var list = new List<T>();
                // foreach (var a in b)
                // {
                //     list.Add(a)
                // }
                if (_semanticModel.GetSymbolInfo(invocationExpression, _cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.Parameters.Length == 0)
                {
                    return TryConvertIfInInvocation(
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
                        invocationExpression,
                        parentStatement,
                        initializer,
                        variableName,
                        out var variable,
                        out var nodesBefore,
                        out var nodesAfter))
                    {
                        if (TryConvertQueryExpression(
                            expression => leafExpressionCreationMethod(variable, expression),
                            out var statements))
                        {
                            var list = new List<CSharpSyntaxNode>();
                            list.AddRange(nodesBefore);
                            list.AddRange(statements);
                            list.AddRange(nodesAfter);
                            documentUpdateInfo = new DocumentUpdateInfo(parentStatement, list);
                            return true;
                        }
                    }
                }

                documentUpdateInfo = default;
                return false;
            }

            private bool TryConvertIfInInvocationInternal(
                InvocationExpressionSyntax invocationExpression,
                StatementSyntax parentStatement,
                ExpressionSyntax initializer,
                string variableName,
                out ExpressionSyntax variable,
                out CSharpSyntaxNode[] nodesBefore,
                out CSharpSyntaxNode[] nodesAfter)
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
                            nodesBefore = new[] { parentStatement.ReplaceNode(invocationExpression, initializer.WithAdditionalAnnotations(Simplifier.Annotation)) };
                            nodesAfter = new CSharpSyntaxNode[] { };
                            return true;
                        }

                        break;
                    case SyntaxKind.SimpleAssignmentExpression:
                        if (((AssignmentExpressionSyntax)invocationParent).Right.WalkDownParentheses() == invocationExpression)
                        {
                            // before a = (from a in b select a).ToList();
                            // after a = new List<T>();
                            // foreach(...)
                            variable = ((AssignmentExpressionSyntax)invocationParent).Left;
                            nodesBefore = new[] { parentStatement.ReplaceNode(invocationExpression, initializer.WithAdditionalAnnotations(Simplifier.Annotation)) };
                            nodesAfter = new CSharpSyntaxNode[] { };
                            return true;
                        }

                        break;
                    case SyntaxKind.ReturnStatement:
                        // before return (from a in b select a).ToList();
                        // after var list = new List<T>();
                        // foreach(...)
                        // return list;
                        var symbolName1 = GetFreeSymbolName(variableName);
                        variable = SyntaxFactory.IdentifierName(symbolName1);
                        nodesBefore = new[] { CreateLocalDeclarationStatement(symbolName1, initializer) };
                        nodesAfter = new[] { SyntaxFactory.ReturnStatement(variable).WithAdditionalAnnotations(Simplifier.Annotation) };
                        return true;
                    case SyntaxKind.Argument:
                        // before return SomeMethod((from a in b select a).ToList());
                        // after var list = new List<T>();
                        // foreach(...)
                        // SomeMethod(list);
                        var symbolName2 = GetFreeSymbolName(variableName);
                        variable = SyntaxFactory.IdentifierName(symbolName2);
                        nodesBefore = new[] { CreateLocalDeclarationStatement(symbolName2, initializer) };
                        nodesAfter = new[] { parentStatement.ReplaceNode(invocationExpression, variable.WithAdditionalAnnotations(Simplifier.Annotation)) };
                        return true;
                        // SyntaxKind.ArrowExpressionClause is not supported
                }

                nodesBefore = default;
                nodesAfter = default;
                variable = default;
                return false;
            }

            private LocalDeclarationStatementSyntax CreateLocalDeclarationStatement(SyntaxToken identifier, ExpressionSyntax initializer)
            {
                return SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                VarNameIdentifier,
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(
                                        identifier,
                                        argumentList: null,
                                        SyntaxFactory.EqualsValueClause(initializer))))).WithAdditionalAnnotations(Simplifier.Annotation);
            }

            private bool TryReplaceWithLocalFunction(SyntaxNode parent, out DocumentUpdateInfo documentUpdateInfo)
            {
                var parentStatement = parent.GetAncestorOrThis<StatementSyntax>();
                if (parentStatement != null)
                {
                    StatementSyntax internalNodeMethod(ExpressionSyntax expression)
                        => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, expression);

                    if (TryConvertQueryExpression(internalNodeMethod, out var convertedFromQueryExpression))
                    {
                        if (TryGetTypeSyntax(_source, out var typeSyntax))
                        {
                            // before statement ... from a in select b ...
                            // after
                            // IEnumerable<T> localFunction()
                            // {
                            //   foreach(var a in b)
                            //   {
                            //       yield return a;
                            //   }
                            // }
                            //  statement ... localFunction();
                            string localFunctionNamePrefix = _semanticFacts.GenerateNameForExpression(
                                _semanticModel,
                                _source,
                                capitalize: false,
                                _cancellationToken);
                            SyntaxToken localFunctionToken = GetFreeSymbolName(localFunctionNamePrefix);
                            var localFunctionDeclaration = SyntaxFactory.LocalFunctionStatement(
                                modifiers: default,
                                returnType: typeSyntax.WithAdditionalAnnotations(Simplifier.SpecialTypeAnnotation),
                                identifier: localFunctionToken,
                                typeParameterList: null,
                                parameterList: SyntaxFactory.ParameterList(),
                                constraintClauses: default,
                                body: SyntaxFactory.Block(
                                    SyntaxFactory.Token(
                                        SyntaxFactory.TriviaList(),
                                        SyntaxKind.OpenBraceToken,
                                        SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine(Environment.NewLine))),
                                    SyntaxFactory.List(convertedFromQueryExpression),
                                    SyntaxFactory.Token(SyntaxKind.CloseBraceToken)),
                                expressionBody: null);

                            var localFunctionInvocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(localFunctionToken)).WithAdditionalAnnotations(Simplifier.Annotation);
                            SyntaxNode newParentExpressionStatement = parentStatement.ReplaceNode(_source, localFunctionInvocation.WithAdditionalAnnotations(Simplifier.Annotation));
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

            private SyntaxToken GetFreeSymbolName(string prefix)
            {
                var freeToken = _semanticFacts.GenerateUniqueName(_semanticModel, _source, containerOpt: null, baseName: prefix, _usedNames, _cancellationToken);
                _usedNames.Add(freeToken.ValueText);
                return freeToken;
            }

            private bool TryConvertIfInForEach(ForEachStatementSyntax forEachStatement, out DocumentUpdateInfo documentUpdateInfo)
            {
                // before foreach(var x in from a in b select a)
                var statement = forEachStatement.Statement;
                StatementSyntax internalNodeMethod(ExpressionSyntax expression)
                {
                    // before 
                    //  foreach (var a in from a in b where a > 5 select a) 
                    //  { 
                    //      dosomething(a); 
                    //  }
                    // after 
                    //  foreach (var a in b)
                    //  {
                    //      if (a > 5)
                    //      {
                    //          dosomething(a); 
                    //      }
                    //  }
                    if (expression is IdentifierNameSyntax identifier &&
                        identifier.Identifier.ValueText == forEachStatement.Identifier.ValueText)
                    {
                        return statement.WithAdditionalAnnotations(Formatter.Annotation);
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
                                forEachStatement.Type,
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(
                                        forEachStatement.Identifier,
                                        argumentList: null,
                                        SyntaxFactory.EqualsValueClause(expression)))));
                        return AddToBlockTop(declaration, statement).WithAdditionalAnnotations(Formatter.Annotation);
                    }
                }

                if (TryConvertQueryExpression(internalNodeMethod, out var convertedFromQueryExpression))
                {
                    documentUpdateInfo = new DocumentUpdateInfo(forEachStatement, convertedFromQueryExpression);
                    return true;
                }

                documentUpdateInfo = default;
                return false;
            }

            private bool TryConvertIfInReturnStatement(ReturnStatementSyntax returnStatement, out DocumentUpdateInfo documentUpdateInfo)
            {
                // before: return from a in b select a;
                // after: 
                // foreach(var a in b)
                // {
                //      yield return a;
                // }
                StatementSyntax internalNodeMethod(ExpressionSyntax expression)
                    => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, expression);

                if (TryConvertQueryExpression(internalNodeMethod, out var convertedFromQueryExpression))
                {
                    documentUpdateInfo = new DocumentUpdateInfo(returnStatement, convertedFromQueryExpression);
                    return true;
                }

                documentUpdateInfo = default;
                return false;
            }

            private bool TryCreateStackFromQueryExpression(out Stack<CSharpSyntaxNode> stack)
            {
                stack = new Stack<CSharpSyntaxNode>();
                stack.Push(_source.FromClause);
                return TryProcessQueryBody(_source.Body, stack);
            }

            private StatementSyntax[] GenerateStatements(
                Func<ExpressionSyntax, StatementSyntax> leafExpressionCreationMethod,
                Stack<CSharpSyntaxNode> stack)
            {
                StatementSyntax statement = default;
                // Executes syntax building methods from bottom to the top of the tree.
                // Process last clause
                if (stack.Any())
                {
                    var node = stack.Pop();
                    if (node is SelectClauseSyntax selectClause)
                    {
                        statement = WrapWithBlock(leafExpressionCreationMethod(selectClause.Expression));
                    }
                    else
                    {
                        throw new ArgumentException("Last node must me the select clause");
                    }
                }

                // Process all other clauses
                List<StatementSyntax> statements = new List<StatementSyntax>();
                while (stack.Any())
                {
                    statement = ProcessClause(stack.Pop(), statement, out StatementSyntax extraStatement);
                    if (extraStatement != null)
                    {
                        statements.Add(extraStatement);
                    }
                }

                // The stack was processed in the reverse order, but the extra statements should be provided in the direct order.
                statements.Reverse();
                statements.Add(statement.WithAdditionalAnnotations(Simplifier.Annotation));
                return statements.ToArray();
            }

            private bool TryConvertQueryExpression(
                Func<ExpressionSyntax, StatementSyntax> leafExpressionCreationMethod,
                out StatementSyntax[] statements)
            {
                statements = default;
                if (TryCreateStackFromQueryExpression(out var stack))
                {
                    statements = GenerateStatements(leafExpressionCreationMethod, stack);
                    return true;
                }

                return false;
            }

            private bool TryProcessQueryBody(QueryBodySyntax queryBody, Stack<CSharpSyntaxNode> stack)
            {
                foreach (var queryClause in queryBody.Clauses)
                {
                    switch (queryClause.Kind())
                    {
                        case SyntaxKind.WhereClause:
                        case SyntaxKind.LetClause:
                        case SyntaxKind.FromClause:
                            stack.Push(queryClause);
                            break;
                        case SyntaxKind.JoinClause:
                            if (((JoinClauseSyntax)queryClause).Into == null) // GroupJoin is not supported
                            {
                                stack.Push(queryClause);
                                break;
                            }

                            return false;
                        // OrderBy is not supported by foreach.
                        default:
                            return false;
                    }
                }

                // GroupClause is not supported by the conversion
                if (queryBody.SelectOrGroup.Kind() != SyntaxKind.SelectClause)
                {
                    return false;
                }

                stack.Push(queryBody.SelectOrGroup);
                return queryBody.Continuation == null || TryProcessQueryBody(queryBody.Continuation.Body, stack);
            }

            private static BlockSyntax AddToBlockTop(StatementSyntax newStatement, StatementSyntax statement)
            {
                if (statement is BlockSyntax block)
                {
                    return block.WithStatements(block.Statements.Insert(0, newStatement));
                }
                else
                {
                    return SyntaxFactory.Block(newStatement, statement);
                }
            }

            private bool IsLocalOrParameterSymbol(IOperation operation)
            {
                if (operation is IConversionOperation conversion && conversion.IsImplicit)
                {
                    return IsLocalOrParameterSymbol(conversion.Operand);
                }

                return operation.Kind == OperationKind.LocalReference || operation.Kind == OperationKind.ParameterReference;
            }

            private static BlockSyntax WrapWithBlock(StatementSyntax statement)
                => statement is BlockSyntax block ? block : SyntaxFactory.Block(statement);
        }
    }
}
