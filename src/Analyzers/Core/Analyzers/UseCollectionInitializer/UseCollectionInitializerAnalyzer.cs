// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    internal sealed class UseCollectionInitializerAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
        TForeachStatementSyntax,
        TVariableDeclaratorSyntax> : AbstractObjectCreationExpressionAnalyzer<
            TExpressionSyntax,
            TStatementSyntax,
            TObjectCreationExpressionSyntax,
            TVariableDeclaratorSyntax,
            Match<TStatementSyntax>>
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TForeachStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        private static readonly ObjectPool<UseCollectionInitializerAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TForeachStatementSyntax, TVariableDeclaratorSyntax>> s_pool
            = SharedPools.Default<UseCollectionInitializerAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TForeachStatementSyntax, TVariableDeclaratorSyntax>>();

        public static ImmutableArray<Match<TStatementSyntax>> Analyze(
            SemanticModel semanticModel,
            ISyntaxFacts syntaxFacts,
            TObjectCreationExpressionSyntax objectCreationExpression,
            bool areCollectionExpressionsSupported,
            CancellationToken cancellationToken)
        {
            var analyzer = s_pool.Allocate();
            analyzer.Initialize(semanticModel, syntaxFacts, objectCreationExpression, areCollectionExpressionsSupported, cancellationToken);
            try
            {
                return analyzer.AnalyzeWorker();
            }
            finally
            {
                analyzer.Clear();
                s_pool.Free(analyzer);
            }
        }

        protected override void AddMatches(ArrayBuilder<Match<TStatementSyntax>> matches)
        {
            // If containing statement is inside a block (e.g. method), than we need to iterate through its child statements.
            // If containing statement is in top-level code, than we need to iterate through child statements of containing compilation unit.
            var containingBlockOrCompilationUnit = _containingStatement.GetRequiredParent();

            // In case of top-level code parent of the statement will be GlobalStatementSyntax,
            // so we need to get its parent in order to get CompilationUnitSyntax
            if (_syntaxFacts.IsGlobalStatement(containingBlockOrCompilationUnit))
            {
                containingBlockOrCompilationUnit = containingBlockOrCompilationUnit.Parent!;
            }

            var foundStatement = false;

            var seenInvocation = false;
            var seenIndexAssignment = false;

            var initializer = _syntaxFacts.GetInitializerOfBaseObjectCreationExpression(_objectCreationExpression);
            if (initializer != null)
            {
                var firstInit = _syntaxFacts.GetExpressionsOfObjectCollectionInitializer(initializer).First();
                seenIndexAssignment = _syntaxFacts.IsElementAccessInitializer(firstInit);
                seenInvocation = !seenIndexAssignment;
            }

            // An indexer can't be used with a collection expression.  So fail out immediately if we see that.
            if (seenIndexAssignment && _analyzeForCollectionExpression)
                return;

            foreach (var child in containingBlockOrCompilationUnit.ChildNodesAndTokens())
            {
                if (child.IsToken)
                    continue;

                var childNode = child.AsNode();
                var extractedChild = _syntaxFacts.IsGlobalStatement(childNode) ? _syntaxFacts.GetStatementOfGlobalStatement(childNode) : childNode;

                if (!foundStatement)
                {
                    if (extractedChild == _containingStatement)
                    {
                        foundStatement = true;
                    }

                    continue;
                }

                if (extractedChild is TExpressionStatementSyntax expressionStatement)
                {
                    if (!seenIndexAssignment)
                    {
                        // Look for a call to Add or AddRange
                        if (TryAnalyzeInvocation(
                                expressionStatement,
                                addName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                                requiredArgumentName: null,
                                out var instance) &&
                            ValuePatternMatches(instance))
                        {
                            seenInvocation = true;
                            matches.Add(new Match<TStatementSyntax>(expressionStatement, UseSpread: false));
                            continue;
                        }
                        else if (
                            _analyzeForCollectionExpression &&
                            TryAnalyzeInvocation(
                                expressionStatement,
                                addName: nameof(List<int>.AddRange),
                                requiredArgumentName: null,
                                out instance))
                        {
                            seenInvocation = true;

                            // AddRange(x) will become `..x` when we make it into a collection expression.
                            matches.Add(new Match<TStatementSyntax>(expressionStatement, UseSpread: true));
                            continue;
                        }
                    }

                    if (!seenInvocation && !_analyzeForCollectionExpression)
                    {
                        if (TryAnalyzeIndexAssignment(expressionStatement, out var instance))
                        {
                            seenIndexAssignment = true;
                            matches.Add(new Match<TStatementSyntax>(expressionStatement, UseSpread: false));
                            continue;
                        }
                    }

                    return;
                }
                else if (extractedChild is TForeachStatementSyntax foreachStatement)
                {
                    // if we're not producing a collection expression, then we cannot convert any foreach'es into
                    // `[..expr]` elements.
                    if (!_analyzeForCollectionExpression)
                        return;

                    _syntaxFacts.GetPartsOfForeachStatement(foreachStatement, out var identifier, out _, out var foreachStatements);
                    if (identifier == default)
                        return;

                    // must be of the form:
                    //
                    //      foreach (var x in expr)
                    //          dest.Add(x)
                    //
                    // By passing 'x' into TryAnalyzeInvocation below, we ensure that it is an enumerated value from `expr`
                    // being added to `dest`.
                    if (foreachStatements.ToImmutableArray() is not [TExpressionStatementSyntax childExpressionStatement] ||
                        !TryAnalyzeInvocation(
                            childExpressionStatement,
                            addName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                            requiredArgumentName: identifier.Text,
                            out var instance) ||
                        !ValuePatternMatches(instance))
                    {
                        return;
                    }

                    // `foreach` will become `..expr` when we make it into a collection expression.
                    matches.Add(new Match<TStatementSyntax>(foreachStatement, UseSpread: true));
                }
                else
                {
                    return;
                }
            }
        }

        protected override bool ShouldAnalyze()
        {
            if (_syntaxFacts.IsObjectMemberInitializer(_syntaxFacts.GetInitializerOfBaseObjectCreationExpression(_objectCreationExpression)))
                return false;

            var type = _semanticModel.GetTypeInfo(_objectCreationExpression, _cancellationToken).Type;
            if (type == null)
                return false;

            var addMethods = _semanticModel.LookupSymbols(
                _objectCreationExpression.SpanStart,
                container: type,
                name: WellKnownMemberNames.CollectionInitializerAddMethodName,
                includeReducedExtensionMethods: true);

            return addMethods.Any(static m => m is IMethodSymbol methodSymbol && methodSymbol.Parameters.Any());
        }

        private bool TryAnalyzeIndexAssignment(
            TExpressionStatementSyntax statement,
            [NotNullWhen(true)] out TExpressionSyntax? instance)
        {
            instance = null;
            if (!_syntaxFacts.SupportsIndexingInitializer(statement.SyntaxTree.Options))
                return false;

            if (!_syntaxFacts.IsSimpleAssignmentStatement(statement))
                return false;

            _syntaxFacts.GetPartsOfAssignmentStatement(statement, out var left, out var right);

            if (!_syntaxFacts.IsElementAccessExpression(left))
                return false;

            // If we're initializing a variable, then we can't reference that variable on the right 
            // side of the initialization.  Rewriting this into a collection initializer would lead
            // to a definite-assignment error.
            if (ExpressionContainsValuePatternOrReferencesInitializedSymbol(right))
                return false;

            // Can't reference the variable being initialized in the arguments of the indexing expression.
            _syntaxFacts.GetPartsOfElementAccessExpression(left, out var elementInstance, out var argumentList);
            var elementAccessArguments = _syntaxFacts.GetArgumentsOfArgumentList(argumentList);
            foreach (var argument in elementAccessArguments)
            {
                if (ExpressionContainsValuePatternOrReferencesInitializedSymbol(argument))
                    return false;

                // An index/range expression implicitly references the value being initialized.  So it cannot be used in the
                // indexing expression.
                var argExpression = _syntaxFacts.GetExpressionOfArgument(argument);
                argExpression = _syntaxFacts.WalkDownParentheses(argExpression);

                if (_syntaxFacts.IsIndexExpression(argExpression) || _syntaxFacts.IsRangeExpression(argExpression))
                    return false;
            }

            instance = elementInstance as TExpressionSyntax;
            return instance != null;
        }

        private bool TryAnalyzeInvocation(
            TExpressionStatementSyntax statement,
            string addName,
            string? requiredArgumentName,
            [NotNullWhen(true)] out TExpressionSyntax? instance)
        {
            instance = null;
            if (_syntaxFacts.GetExpressionOfExpressionStatement(statement) is not TInvocationExpressionSyntax invocationExpression)
                return false;

            var arguments = _syntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
            if (arguments.Count < 1)
                return false;

            // Collection expressions can only call the single argument Add/AddRange methods on a type.
            // So if we don't have exactly one argument, fail out.
            if (_analyzeForCollectionExpression && arguments.Count != 1)
                return false;

            if (requiredArgumentName != null && arguments.Count != 1)
                return false;

            foreach (var argument in arguments)
            {
                if (!_syntaxFacts.IsSimpleArgument(argument))
                    return false;

                var argumentExpression = _syntaxFacts.GetExpressionOfArgument(argument);
                if (ExpressionContainsValuePatternOrReferencesInitializedSymbol(argumentExpression))
                    return false;

                // VB allows for a collection initializer to be an argument.  i.e. `Goo({a, b, c})`.  This argument
                // cannot be used in an outer collection initializer as it would change meaning.  i.e.:
                //
                //      new List(Of IEnumerable(Of String)) { { a, b, c } }
                //
                // is not legal.  That's because instead of adding `{ a, b, c }` as a single element to the list, VB
                // instead looks for an 3-argument `Add` method to invoke on `List<T>` (which clearly fails).
                if (_syntaxFacts.SyntaxKinds.CollectionInitializerExpression == argumentExpression.RawKind)
                    return false;

                // If the caller is requiring a particular argument name, then validate that is what this argument
                // is referencing.
                if (requiredArgumentName != null)
                {
                    if (!_syntaxFacts.IsIdentifierName(argumentExpression))
                        return false;

                    _syntaxFacts.GetNameAndArityOfSimpleName(argumentExpression, out var suppliedName, out _);
                    if (requiredArgumentName != suppliedName)
                        return false;
                }
            }

            if (_syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression) is not TMemberAccessExpressionSyntax memberAccess)
                return false;

            if (!_syntaxFacts.IsSimpleMemberAccessExpression(memberAccess))
                return false;

            _syntaxFacts.GetPartsOfMemberAccessExpression(memberAccess, out var localInstance, out var memberName);
            _syntaxFacts.GetNameAndArityOfSimpleName(memberName, out var name, out var arity);

            if (arity != 0 || !Equals(name, addName))
                return false;

            instance = localInstance as TExpressionSyntax;
            return instance != null;
        }
    }
}
