// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    internal class UseCollectionInitializerAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
        TVariableDeclaratorSyntax> : AbstractObjectCreationExpressionAnalyzer<
            TExpressionSyntax,
            TStatementSyntax,
            TObjectCreationExpressionSyntax,
            TVariableDeclaratorSyntax,
            TExpressionStatementSyntax>
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        private static readonly ObjectPool<UseCollectionInitializerAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TVariableDeclaratorSyntax>> s_pool
            = SharedPools.Default<UseCollectionInitializerAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TVariableDeclaratorSyntax>>();

        public static ImmutableArray<TExpressionStatementSyntax>? Analyze(
            SemanticModel semanticModel,
            ISyntaxFacts syntaxFacts,
            TObjectCreationExpressionSyntax objectCreationExpression,
            CancellationToken cancellationToken)
        {
            var analyzer = s_pool.Allocate();
            analyzer.Initialize(semanticModel, syntaxFacts, objectCreationExpression, cancellationToken);
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

        protected override void AddMatches(ArrayBuilder<TExpressionStatementSyntax> matches)
        {
            var containingBlock = _containingStatement.GetRequiredParent();
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

            foreach (var child in containingBlock.ChildNodesAndTokens())
            {
                if (!foundStatement)
                {
                    if (child == _containingStatement)
                    {
                        foundStatement = true;
                    }

                    continue;
                }

                if (child.IsToken)
                    return;

                if (child.AsNode() is not TExpressionStatementSyntax statement)
                    return;

                SyntaxNode? instance = null;
                if (!seenIndexAssignment && TryAnalyzeAddInvocation(statement, out instance))
                    seenInvocation = true;

                if (!seenInvocation && TryAnalyzeIndexAssignment(statement, out instance))
                    seenIndexAssignment = true;

                if (instance == null)
                    return;

                if (!ValuePatternMatches((TExpressionSyntax)instance))
                    return;

                matches.Add(statement);
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

            return addMethods.Any(m => m is IMethodSymbol methodSymbol && methodSymbol.Parameters.Any());
        }

        private bool TryAnalyzeIndexAssignment(
            TExpressionStatementSyntax statement,
            [NotNullWhen(true)] out SyntaxNode? instance)
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

            instance = elementInstance;
            return instance != null;
        }

        private bool TryAnalyzeAddInvocation(
            TExpressionStatementSyntax statement,
            [NotNullWhen(true)] out SyntaxNode? instance)
        {
            instance = null;
            if (_syntaxFacts.GetExpressionOfExpressionStatement(statement) is not TInvocationExpressionSyntax invocationExpression)
                return false;

            var arguments = _syntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
            if (arguments.Count < 1)
                return false;

            foreach (var argument in arguments)
            {
                if (!_syntaxFacts.IsSimpleArgument(argument))
                    return false;

                var argumentExpression = _syntaxFacts.GetExpressionOfArgument(argument);
                if (ExpressionContainsValuePatternOrReferencesInitializedSymbol(argumentExpression))
                    return false;
            }

            if (_syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression) is not TMemberAccessExpressionSyntax memberAccess)
                return false;

            if (!_syntaxFacts.IsSimpleMemberAccessExpression(memberAccess))
                return false;

            _syntaxFacts.GetPartsOfMemberAccessExpression(memberAccess, out var localInstance, out var memberName);
            _syntaxFacts.GetNameAndArityOfSimpleName(memberName, out var name, out var arity);

            if (arity != 0 || !Equals(name, WellKnownMemberNames.CollectionInitializerAddMethodName))
                return false;

            instance = localInstance;
            return true;
        }
    }
}
