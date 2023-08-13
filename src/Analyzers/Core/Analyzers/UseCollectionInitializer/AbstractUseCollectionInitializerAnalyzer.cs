// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    using static UseCollectionInitializerHelpers;
    using static UpdateObjectCreationHelpers;

    internal abstract class AbstractUseCollectionInitializerAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
        TForeachStatementSyntax,
        TIfStatementSyntax,
        TVariableDeclaratorSyntax,
        TAnalyzer> : AbstractObjectCreationExpressionAnalyzer<
            TExpressionSyntax,
            TStatementSyntax,
            TObjectCreationExpressionSyntax,
            TVariableDeclaratorSyntax,
            Match<TStatementSyntax>>, IDisposable
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TForeachStatementSyntax : TStatementSyntax
        where TIfStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
        where TAnalyzer : AbstractUseCollectionInitializerAnalyzer<
            TExpressionSyntax,
            TStatementSyntax,
            TObjectCreationExpressionSyntax,
            TMemberAccessExpressionSyntax,
            TInvocationExpressionSyntax,
            TExpressionStatementSyntax,
            TForeachStatementSyntax,
            TIfStatementSyntax,
            TVariableDeclaratorSyntax,
            TAnalyzer>, new()
    {
        private static readonly ObjectPool<TAnalyzer> s_pool = SharedPools.Default<TAnalyzer>();

        protected abstract bool IsComplexElementInitializer(SyntaxNode expression);
        protected abstract bool HasExistingInvalidInitializerForCollection(TObjectCreationExpressionSyntax objectCreation);

        protected abstract void GetPartsOfForeachStatement(TForeachStatementSyntax statement, out SyntaxToken identifier, out TExpressionSyntax expression, out IEnumerable<TStatementSyntax> statements);
        protected abstract void GetPartsOfIfStatement(TIfStatementSyntax statement, out TExpressionSyntax condition, out IEnumerable<TStatementSyntax> whenTrueStatements, out IEnumerable<TStatementSyntax>? whenFalseStatements);

        public static TAnalyzer Allocate()
            => s_pool.Allocate();

        public void Dispose()
        {
            this.Clear();
            s_pool.Free((TAnalyzer)this);
        }

        public ImmutableArray<Match<TStatementSyntax>> Analyze(
            SemanticModel semanticModel,
            ISyntaxFacts syntaxFacts,
            TObjectCreationExpressionSyntax objectCreationExpression,
            bool areCollectionExpressionsSupported,
            CancellationToken cancellationToken)
        {
            this.Initialize(semanticModel, syntaxFacts, objectCreationExpression, areCollectionExpressionsSupported, cancellationToken);
            var result = this.AnalyzeWorker();

            // If analysis failed entirely, immediately bail out.
            if (result.IsDefault)
                return default;

            // Analysis succeeded, but the result may be empty or non empty.
            //
            // For collection expressions, it's fine for this result to be empty.  In other words, it's ok to offer
            // changing `new List<int>() { 1 }` (on its own) to `[1]`.
            //
            // However, for collection initializers we always want at least one element to add to the initializer.  In
            // other words, we don't want to suggest changing `new List<int>()` to `new List<int>() { }` as that's just
            // noise.  So convert empty results to an invalid result here.
            if (areCollectionExpressionsSupported)
                return result;

            // Downgrade an empty result to a failure for the normal collection-initializer case.
            return result.IsEmpty ? default : result;
        }

        protected override bool TryAddMatches(ArrayBuilder<Match<TStatementSyntax>> matches)
        {
            var seenInvocation = false;
            var seenIndexAssignment = false;

            var initializer = _syntaxFacts.GetInitializerOfBaseObjectCreationExpression(_objectCreationExpression);
            if (initializer != null)
            {
                var initializerExpressions = _syntaxFacts.GetExpressionsOfObjectCollectionInitializer(initializer);
                if (initializerExpressions is [var firstInit, ..])
                {
                    // if we have an object creation, and it *already* has an initializer in it (like `new T { { x, y } }`)
                    // this can't legally become a collection expression.
                    if (_analyzeForCollectionExpression && this.IsComplexElementInitializer(firstInit))
                        return false;

                    seenIndexAssignment = _syntaxFacts.IsElementAccessInitializer(firstInit);
                    seenInvocation = !seenIndexAssignment;

                    // An indexer can't be used with a collection expression.  So fail out immediately if we see that.
                    if (seenIndexAssignment && _analyzeForCollectionExpression)
                        return false;
                }
            }

            foreach (var statement in GetSubsequentStatements(_syntaxFacts, _containingStatement))
            {
                var match = TryAnalyzeStatement(statement, ref seenInvocation, ref seenIndexAssignment);
                if (match is null)
                    break;

                matches.Add(match.Value);
            }

            return true;
        }

        private Match<TStatementSyntax>? TryAnalyzeStatement(TStatementSyntax statement, ref bool seenInvocation, ref bool seenIndexAssignment)
        {
            return _analyzeForCollectionExpression
                ? TryAnalyzeStatementForCollectionExpression(statement)
                : TryAnalyzeStatementForCollectionInitializer(statement, ref seenInvocation, ref seenIndexAssignment);
        }

        private Match<TStatementSyntax>? TryAnalyzeStatementForCollectionExpression(TStatementSyntax statement)
        {
            return statement switch
            {
                TExpressionStatementSyntax expressionStatement => TryAnalyzeExpressionStatement(expressionStatement),
                TForeachStatementSyntax foreachStatement => TryAnalyzeForeachStatement(foreachStatement),
                TIfStatementSyntax ifStatement => TryAnalyzeIfStatement(ifStatement),
                _ => null,
            };

            Match<TStatementSyntax>? TryAnalyzeExpressionStatement(TExpressionStatementSyntax expressionStatement)
            {
                // Look for a call to Add or AddRange
                if (TryAnalyzeInvocation(
                        expressionStatement,
                        addName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                        requiredArgumentName: null,
                        out var instance) &&
                    ValuePatternMatches(_syntaxFacts, _valuePattern, instance))
                {
                    return new Match<TStatementSyntax>(expressionStatement, UseSpread: false);
                }

                if (TryAnalyzeInvocation(
                        expressionStatement,
                        addName: nameof(List<int>.AddRange),
                        requiredArgumentName: null,
                        out instance))
                {
                    // AddRange(x) will become `..x` when we make it into a collection expression.
                    return new Match<TStatementSyntax>(expressionStatement, UseSpread: true);
                }

                return null;
            }

            Match<TStatementSyntax>? TryAnalyzeForeachStatement(TForeachStatementSyntax foreachStatement)
            {
                this.GetPartsOfForeachStatement(foreachStatement, out var identifier, out _, out var foreachStatements);
                // must be of the form:
                //
                //      foreach (var x in expr)
                //          dest.Add(x)
                //
                // By passing 'x' into TryAnalyzeInvocation below, we ensure that it is an enumerated value from `expr`
                // being added to `dest`.
                if (foreachStatements.ToImmutableArray() is [TExpressionStatementSyntax childExpressionStatement] &&
                    TryAnalyzeInvocation(
                        childExpressionStatement,
                        addName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                        requiredArgumentName: identifier.Text,
                        out var instance) &&
                    ValuePatternMatches(_syntaxFacts, _valuePattern, instance))
                {
                    // `foreach` will become `..expr` when we make it into a collection expression.
                    return new Match<TStatementSyntax>(foreachStatement, UseSpread: true);
                }

                return null;
            }

            Match<TStatementSyntax>? TryAnalyzeIfStatement(TIfStatementSyntax ifStatement)
            {
                // look for the form:
                //
                //  if (x)
                //      expr.Add(y)
                //
                // or
                //
                //  if (x)
                //      expr.Add(y)
                //  else
                //      expr.Add(z)

                this.GetPartsOfIfStatement(ifStatement, out _, out var whenTrue, out var whenFalse);
                var whenTrueStatements = whenTrue.ToImmutableArray();

                if (whenTrueStatements is [TExpressionStatementSyntax trueChildStatement] &&
                    TryAnalyzeInvocation(
                        trueChildStatement,
                        addName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                        requiredArgumentName: null,
                        out var instance) &&
                    ValuePatternMatches(_syntaxFacts, _valuePattern,    instance))
                {
                    if (whenFalse is null)
                    {
                        // add the form `.. x ? [y] : []` to the result
                        return new Match<TStatementSyntax>(ifStatement, UseSpread: true);
                    }

                    var whenFalseStatements = whenFalse.ToImmutableArray();
                    if (whenFalseStatements is [TExpressionStatementSyntax falseChildStatement] &&
                        TryAnalyzeInvocation(
                            falseChildStatement,
                            addName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                            requiredArgumentName: null,
                            out instance) &&
                        ValuePatternMatches(_syntaxFacts, _valuePattern, instance))
                    {
                        // add the form `x ? y : z` to the result
                        return new Match<TStatementSyntax>(ifStatement, UseSpread: false);
                    }
                }

                return null;
            }
        }

        private Match<TStatementSyntax>? TryAnalyzeStatementForCollectionInitializer(
            TStatementSyntax statement, ref bool seenInvocation, ref bool seenIndexAssignment)
        {
            // At least one of these has to be false.
            Contract.ThrowIfTrue(seenInvocation && seenIndexAssignment);

            if (statement is not TExpressionStatementSyntax expressionStatement)
                return null;

            // Can't mix Adds and indexing.
            if (!seenIndexAssignment)
            {
                // Look for a call to Add or AddRange
                if (TryAnalyzeInvocation(
                        expressionStatement,
                        addName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                        requiredArgumentName: null,
                        out var instance) &&
                    ValuePatternMatches(_syntaxFacts, _valuePattern, instance))
                {
                    seenInvocation = true;
                    return new Match<TStatementSyntax>(expressionStatement, UseSpread: false);
                }
            }

            if (!seenInvocation)
            {
                if (TryAnalyzeIndexAssignment(expressionStatement, out var instance) &&
                    ValuePatternMatches(_syntaxFacts, _valuePattern, instance))
                {
                    seenIndexAssignment = true;
                    return new Match<TStatementSyntax>(expressionStatement, UseSpread: false);
                }
            }

            return null;
        }

        protected override bool ShouldAnalyze()
        {
            if (this.HasExistingInvalidInitializerForCollection(_objectCreationExpression))
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
