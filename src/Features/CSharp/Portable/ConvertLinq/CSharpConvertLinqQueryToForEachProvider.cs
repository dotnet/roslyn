// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
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
            private readonly List<string> _introducedLocalNames;

            public Converter(SemanticModel semanticModel, ISemanticFactsService semanticFacts, QueryExpressionSyntax source, CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _semanticFacts = semanticFacts;
                _source = source;
                _introducedLocalNames = new List<string>();
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

                // Bail out if there is no chance to convert it even with a local function.
                if (!CanTryConvertToLocalFunction() ||
                    !TryCreateStackFromQueryExpression(out QueryExpressionProcessingInfo queryExpressionProcessingInfo))
                {
                    documentUpdateInfo = default;
                    return false;
                }

                // GetDiagnostics is expensive. Move it to the end if there were no bail outs from the algorithm.
                // TODO likely adding more semantic checks will perform checks we expect from GetDiagnostics
                // We may consider removing GetDiagnostics.
                // https://github.com/dotnet/roslyn/issues/25639
                if ((TryConvertInternal(queryExpressionProcessingInfo, out documentUpdateInfo) ||
                    TryReplaceWithLocalFunction(out documentUpdateInfo)) && // second attempt: at least to a local function
                    !_semanticModel.GetDiagnostics(_source.Span, _cancellationToken).Any())
                {
                    return true;
                }

                documentUpdateInfo = default;
                return false;
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
                                SyntaxToken variable = GetFreeSymbolNameAndMarkUsed(expressionName);
                                extraStatementToAddAbove = CreateLocalDeclarationStatement(variable, joinClause.InExpression);

                                // Replace YY() with yy declared above.
                                expression = SyntaxFactory.IdentifierName(variable);
                            }

                            // Output for the join
                            // var yy == YY(); this goes to extraStatementToAddAbove
                            // ...
                            // foreach (var y in yy)
                            // {
                            //  if (object.Equals(x, y))
                            //  {
                            //      ...
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

            private bool TryConvertInternal(QueryExpressionProcessingInfo queryExpressionProcessingInfo, out DocumentUpdateInfo documentUpdateInfo)
            {
                // (from a in b select a); 
                SyntaxNode parent = _source.WalkUpParentheses().Parent;

                switch (parent.Kind())
                {
                    // return from a in b select a;
                    case SyntaxKind.ReturnStatement:
                        return TryConvertIfInReturnStatement((ReturnStatementSyntax)parent, queryExpressionProcessingInfo, out documentUpdateInfo);
                    // foreach(var x in from a in b select a)
                    case SyntaxKind.ForEachStatement:
                        return TryConvertIfInForEach((ForEachStatementSyntax)parent, queryExpressionProcessingInfo, out documentUpdateInfo);
                    // (from a in b select a).ToList(), (from a in b select a).Count(), etc.
                    case SyntaxKind.SimpleMemberAccessExpression:
                        return TryConvertIfInMemberAccessExpression((MemberAccessExpressionSyntax)parent, out documentUpdateInfo);
                }

                documentUpdateInfo = default;
                return false;
            }

            /// <summary>
            /// Checks if the location of the query expression allows to convert it at least to a local function.
            /// It still does not guarantees that the conversion can be performed. There can be bail outs of later stages.
            /// </summary>
            /// <returns></returns>
            private bool CanTryConvertToLocalFunction()
            {
                SyntaxNode currentNode = _source;
                while (currentNode != null)
                {
                    switch (currentNode.Kind())
                    {
                        case SyntaxKind.CatchClause:
                        case SyntaxKind.WhenClause:
                        case SyntaxKind.ArrowExpressionClause: // This one can be considered for support later.
                            return false;
                    }

                    // Should have statements above the query expression to perform the conversion.
                    if (currentNode is MemberDeclarationSyntax) { return false; }
                    if (currentNode is StatementSyntax) { return true; }

                    currentNode = currentNode.Parent;
                }

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
                    !methodSymbol.ReturnsByRef &&
                    // TODO the check commented below does not work
                    //SymbolEquivalenceComparer.Instance.Equals(methodSymbol.ReturnType.OriginalDefinition.Name, _semanticModel.Compilation.Assembly.GetTypeByMetadataName(typeof(List<>).FullName)) && 
                    methodSymbol.ReturnType.OriginalDefinition.Name == nameof(List<object>) &&
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
                            // TODO check if localOrParameter. if not, introduce a new local.
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
                            // TODO check if localOrParameter. if not, introduce a new local.
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
                        var symbolName1 = GetFreeSymbolNameAndMarkUsed(variableName);
                        variable = SyntaxFactory.IdentifierName(symbolName1);
                        nodesBefore = new[] { CreateLocalDeclarationStatement(symbolName1, initializer) };
                        nodesAfter = new[] { SyntaxFactory.ReturnStatement(variable).WithAdditionalAnnotations(Simplifier.Annotation) };
                        return true;
                    case SyntaxKind.Argument:
                        // before return SomeMethod((from a in b select a).ToList());
                        // after var list = new List<T>();
                        // foreach(...)
                        // SomeMethod(list);
                        var symbolName2 = GetFreeSymbolNameAndMarkUsed(variableName);
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

            private bool TryReplaceWithLocalFunction(out DocumentUpdateInfo documentUpdateInfo)
            {
                var parentStatement = _source.GetAncestorOrThis<StatementSyntax>();
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
                            SyntaxToken localFunctionToken = GetFreeSymbolNameAndMarkUsed(localFunctionNamePrefix);
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
                            SyntaxNode newParentExpressionStatement = parentStatement.ReplaceNode(_source.WalkUpParentheses(), localFunctionInvocation.WithAdditionalAnnotations(Simplifier.Annotation));
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

            private SyntaxToken GetFreeSymbolNameAndMarkUsed(string prefix)
            {
                var freeToken = _semanticFacts.GenerateUniqueName(_semanticModel, _source, containerOpt: null, baseName: prefix, _introducedLocalNames, _cancellationToken);
                _introducedLocalNames.Add(freeToken.ValueText);
                return freeToken;
            }

            private bool TryConvertIfInForEach(
                ForEachStatementSyntax forEachStatement,
                QueryExpressionProcessingInfo queryExpressionProcessingInfo,
                out DocumentUpdateInfo documentUpdateInfo)
            {
                // before foreach(var x in from a in b select a)

                if (forEachStatement.Expression.WalkDownParentheses() != _source)
                {
                    documentUpdateInfo = default;
                    return false;
                }

                // check that the body of the forEach does not contain any identifiers from the query
                foreach (var identifier in queryExpressionProcessingInfo.Identifiers)
                {
                    // Identifier from the foreach can already be in scope of the foreach statement.
                    if (forEachStatement.Identifier.ValueText != identifier.ValueText)
                    {
                        var name = identifier.ValueText;
                        if (_semanticFacts.GenerateUniqueName(
                            _semanticModel,
                            location: forEachStatement.Statement,
                            containerOpt: forEachStatement.Statement,
                            baseName: name,
                            usedNames: Enumerable.Empty<string>(), _cancellationToken).ValueText != name)
                        {
                            documentUpdateInfo = default;
                            return false;
                        }
                    }
                }

                // If query does not contains identifier with the same name as declared in the foreach,
                // declare this identifier in the body.
                if (!queryExpressionProcessingInfo.ContainsIdentifier(forEachStatement.Identifier))
                {
                    documentUpdateInfo = ConvertIfInToForeachWithExtraVariableDeclaration(forEachStatement, queryExpressionProcessingInfo);
                    return true;
                }
                else
                {
                    // The last select expression in the query returns this identifier:
                    // foreach(var thisIdentifier in from ....... select thisIdentifier)
                    // if thisIdentifier in foreach is var, it is OK
                    // if a type is specified for thisIdentifier in forEach, check that the type is the same as in the select expression
                    // foreach(MyType thisIdentifier in from ....... from MyType thisIdentifier ... select thisIdentifier)

                    // The last clause in query stack must be SelectClauseSyntax.
                    var lastSelectExpression = ((SelectClauseSyntax)queryExpressionProcessingInfo.Stack.Peek()).Expression;
                    if (lastSelectExpression is IdentifierNameSyntax identifierName)
                    {
                        if (forEachStatement.Identifier.ValueText == identifierName.Identifier.ValueText)
                        {
                            var forEachStatementTypeSymbol = _semanticModel.GetTypeInfo(forEachStatement.Type, _cancellationToken).Type;
                            var lastSelectExpressionTypeSymbol = _semanticModel.GetOperation(lastSelectExpression, _cancellationToken).Type;
                            if (SymbolEquivalenceComparer.Instance.Equals(forEachStatementTypeSymbol, lastSelectExpressionTypeSymbol))
                            {
                                documentUpdateInfo = ConvertIfInToForeachWithoutExtraVariableDeclaration(forEachStatement, queryExpressionProcessingInfo);
                                return true;
                            }
                        }
                    }
                }

                // in all other cases try to replace with a local function - this is called above.
                documentUpdateInfo = default;
                return false;
            }

            private DocumentUpdateInfo ConvertIfInToForeachWithExtraVariableDeclaration(
                ForEachStatementSyntax forEachStatement,
                QueryExpressionProcessingInfo queryExpressionProcessingInfo)
            {
                // before foreach(var x in from ... a) { dosomething(x); }
                // after 
                // foreach (var a in ...)
                // ...
                // {
                //      var x = a;
                //      dosomething(x); 
                //  }
                var statements = GenerateStatements(
                    expression => AddToBlockTop(SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        forEachStatement.Type,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                forEachStatement.Identifier,
                                argumentList: null,
                                SyntaxFactory.EqualsValueClause(expression))))),
                                forEachStatement.Statement).WithAdditionalAnnotations(Formatter.Annotation), queryExpressionProcessingInfo);
                return new DocumentUpdateInfo(forEachStatement, statements);
            }

            private DocumentUpdateInfo ConvertIfInToForeachWithoutExtraVariableDeclaration(
                ForEachStatementSyntax forEachStatement,
                QueryExpressionProcessingInfo queryExpressionProcessingInfo)
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
                var statements = GenerateStatements(
                    expression => forEachStatement.Statement.WithAdditionalAnnotations(Formatter.Annotation),
                    queryExpressionProcessingInfo);
                return new DocumentUpdateInfo(forEachStatement, statements);
            }

            private bool TryConvertIfInReturnStatement(
                ReturnStatementSyntax returnStatement,
                QueryExpressionProcessingInfo queryExpressionProcessingInfo,
                out DocumentUpdateInfo documentUpdateInfo)
            {
                // The conversion requires yield return which cannot be added to lambdas and anonymous method declarations.
                if (IsWithinImmediateLambdaOrAnonymousMethod(returnStatement))
                {
                    documentUpdateInfo = default;
                    return false;
                }

                var memberDeclarationNode = FindParentMemberDeclarationNode(returnStatement);
                if (memberDeclarationNode == null)
                {
                    throw new ArgumentException("Must bail out on previous steps");
                }

                var declaredSymbol = _semanticModel.GetDeclaredSymbol(memberDeclarationNode);
                ITypeSymbol typeSymbol;
                switch (declaredSymbol.Kind)
                {
                    case SymbolKind.Local:
                        typeSymbol = ((ILocalSymbol)declaredSymbol).Type;
                        break;
                    case SymbolKind.Method:
                        typeSymbol = ((IMethodSymbol)declaredSymbol).ReturnType;
                        break;
                    case SymbolKind.Property:
                        typeSymbol = ((IPropertySymbol)declaredSymbol).Type;
                        break;
                    default:
                        throw new ArgumentException(declaredSymbol.Kind.ToString());
                }

                if (typeSymbol.OriginalDefinition?.SpecialType != SpecialType.System_Collections_Generic_IEnumerable_T)
                {
                    documentUpdateInfo = default;
                    return false;
                }

                // before: return from a in b select a;
                // after: 
                // foreach(var a in b)
                // {
                //      yield return a;
                // }
                var statements = GenerateStatements((ExpressionSyntax expression)
                    => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, expression), queryExpressionProcessingInfo);


                // if there are more than one return in the method, add yield break;
                if (memberDeclarationNode.DescendantNodes().OfType<ReturnStatementSyntax>().Count() != 1)
                {
                    var yieldBreakStatement = SyntaxFactory.YieldStatement(SyntaxKind.YieldBreakStatement);
                    documentUpdateInfo = new DocumentUpdateInfo(returnStatement, statements.Concat(new[] { yieldBreakStatement }));
                }
                else
                {
                    documentUpdateInfo = new DocumentUpdateInfo(returnStatement, statements);
                }

                return true;
            }

            private static SyntaxNode FindParentMemberDeclarationNode(SyntaxNode node)
            {
                while (node != null)
                {
                    switch (node.Kind())
                    {
                        case SyntaxKind.MethodDeclaration:
                        case SyntaxKind.LocalFunctionStatement:
                        case SyntaxKind.PropertyDeclaration:
                        case SyntaxKind.AnonymousMethodExpression:
                            return node;
                    }

                    node = node.Parent;
                }

                return null;
            }

            private bool TryCreateStackFromQueryExpression(out QueryExpressionProcessingInfo queryExpressionProcessingInfo)
            {
                queryExpressionProcessingInfo = new QueryExpressionProcessingInfo(_source.FromClause);
                return TryProcessQueryBody(_source.Body, queryExpressionProcessingInfo);
            }

            private StatementSyntax[] GenerateStatements(
                Func<ExpressionSyntax, StatementSyntax> leafExpressionCreationMethod,
                QueryExpressionProcessingInfo queryExpressionProcessingInfo)
            {
                StatementSyntax statement = default;
                var stack = queryExpressionProcessingInfo.Stack;
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

            // TODO remove this method: should be split into 2 or refactored
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

            private bool TryProcessQueryBody(QueryBodySyntax queryBody, QueryExpressionProcessingInfo queryExpressionProcessingInfo)
            {
                foreach (var queryClause in queryBody.Clauses)
                {
                    switch (queryClause.Kind())
                    {
                        case SyntaxKind.WhereClause:
                            queryExpressionProcessingInfo.Add(queryClause);
                            break;
                        case SyntaxKind.LetClause:
                            if (!queryExpressionProcessingInfo.TryAdd(queryClause, ((LetClauseSyntax)queryClause).Identifier))
                            {
                                return false;
                            }

                            break;
                        case SyntaxKind.FromClause:
                            var fromClause = (FromClauseSyntax)queryClause;
                            if (!queryExpressionProcessingInfo.TryAdd(queryClause, fromClause.Identifier))
                            {
                                return false;
                            }

                            break;
                        case SyntaxKind.JoinClause:
                            var joinClause = (JoinClauseSyntax)queryClause;
                            if (joinClause.Into == null) // GroupJoin is not supported
                            {
                                if (queryExpressionProcessingInfo.TryAdd(joinClause, joinClause.Identifier))
                                {
                                    break;
                                }
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

                queryExpressionProcessingInfo.Add(queryBody.SelectOrGroup);
                return queryBody.Continuation == null || TryProcessQueryBody(queryBody.Continuation.Body, queryExpressionProcessingInfo);
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

            // Checks if the node is within an immediate lambda or within an immediate anonymous method.
            // 'lambda => node' returns true
            // 'lambda => localfunction => node' returns false
            // 'member => node' returns false
            private static bool IsWithinImmediateLambdaOrAnonymousMethod(SyntaxNode node)
            {
                while (node != null)
                {
                    switch (node.Kind())
                    {
                        case SyntaxKind.AnonymousMethodExpression:
                        case SyntaxKind.ParenthesizedLambdaExpression:
                        case SyntaxKind.SimpleLambdaExpression:
                            return true;
                        case SyntaxKind.LocalFunctionStatement:
                            return false;
                        default:
                            if (node is MemberDeclarationSyntax)
                            {
                                return false;
                            }

                            break;
                    }

                    node = node.Parent;
                }

                return false;
            }

            private class QueryExpressionProcessingInfo
            {
                public Stack<CSharpSyntaxNode> Stack { get; private set; }

                public List<SyntaxToken> Identifiers { get; private set; }

                public QueryExpressionProcessingInfo(FromClauseSyntax fromClause)
                {
                    Stack = new Stack<CSharpSyntaxNode>();
                    Stack.Push(fromClause);
                    Identifiers = new List<SyntaxToken>();
                    Identifiers.Add((fromClause.Identifier));
                }

                public bool TryAdd(CSharpSyntaxNode node, SyntaxToken identifier)
                {
                    // Duplicate identifiers are not allowed.
                    // var q = from x in new[] { 1 } select x + 2 into x where x > 0 select 7 into y let x = ""aaa"" select x;
                    if (ContainsIdentifier(identifier))
                    {
                        return false;
                    }

                    Identifiers.Add((identifier));
                    Stack.Push(node);
                    return true;
                }

                public void Add(CSharpSyntaxNode node) => Stack.Push(node);

                public bool ContainsIdentifier(SyntaxToken identifier)
                    => Identifiers.Any(existingIdentifier => existingIdentifier.ValueText == identifier.ValueText);
            }
        }
    }
}
