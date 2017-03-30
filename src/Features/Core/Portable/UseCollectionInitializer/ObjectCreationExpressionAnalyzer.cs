// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    internal class ObjectCreationExpressionAnalyzer<
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
        private static readonly ObjectPool<ObjectCreationExpressionAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TVariableDeclaratorSyntax>> s_pool
            = new ObjectPool<ObjectCreationExpressionAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TVariableDeclaratorSyntax>>(
                () => new ObjectCreationExpressionAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TVariableDeclaratorSyntax>());

        public static ObjectCreationExpressionAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TVariableDeclaratorSyntax> Allocate()
            => s_pool.Allocate();

        private ObjectCreationExpressionAnalyzer()
        {
        }

        public void Free()
        {
            this.Clear();
            s_pool.Free(this);
        }

        protected override void AddMatches(ArrayBuilder<TExpressionStatementSyntax> matches)
        {
            var containingBlock = _containingStatement.Parent;
            var foundStatement = false;

            var seenInvocation = false;
            var seenIndexAssignment = false;

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
                {
                    return;
                }

                var statement = child.AsNode() as TExpressionStatementSyntax;
                if (statement == null)
                {
                    return;
                }

                SyntaxNode instance = null;
                if (!seenIndexAssignment)
                {
                    if (TryAnalyzeAddInvocation(statement, out instance))
                    {
                        seenInvocation = true;
                    }
                }

                if (!seenInvocation)
                {
                    if (TryAnalyzeIndexAssignment(statement, out instance))
                    {
                        seenIndexAssignment = true;
                    }
                }

                if (instance == null)
                {
                    return;
                }

                if (!ValuePatternMatches((TExpressionSyntax)instance))
                {
                    return;
                }

                matches.Add(statement);
            }
        }

        private bool TryAnalyzeIndexAssignment(
            TExpressionStatementSyntax statement,
            out SyntaxNode instance)
        {
            instance = null;
            if (!_syntaxFacts.SupportsIndexingInitializer(statement.SyntaxTree.Options))
            {
                return false;
            }

            if (!_syntaxFacts.IsSimpleAssignmentStatement(statement))
            {
                return false;
            }

            _syntaxFacts.GetPartsOfAssignmentStatement(statement,
                out var left, out var right);

            if (!_syntaxFacts.IsElementAccessExpression(left))
            {
                return false;
            }

            // If we're initializing a variable, then we can't reference that variable on the right 
            // side of the initialization.  Rewriting this into a collection initializer would lead
            // to a definite-assignment error.
            if (ExpressionContainsValuePatternOrReferencesInitializedSymbol(right))
            {
                return false;
            }

            instance = _syntaxFacts.GetExpressionOfElementAccessExpression(left);
            return true;
        }

        private bool TryAnalyzeAddInvocation(
            TExpressionStatementSyntax statement,
            out SyntaxNode instance)
        {
            instance = null;
            var invocationExpression = _syntaxFacts.GetExpressionOfExpressionStatement(statement) as TInvocationExpressionSyntax;
            if (invocationExpression == null)
            {
                return false;
            }

            var arguments = _syntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
            if (arguments.Count < 1)
            {
                return false;
            }

            foreach (var argument in arguments)
            {
                if (!_syntaxFacts.IsSimpleArgument(argument))
                {
                    return false;
                }

                var argumentExpression = _syntaxFacts.GetExpressionOfArgument(argument);
                if (ExpressionContainsValuePatternOrReferencesInitializedSymbol(argumentExpression))
                {
                    return false;
                }
            }

            var memberAccess = _syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression) as TMemberAccessExpressionSyntax;
            if (memberAccess == null)
            {
                return false;
            }

            if (!_syntaxFacts.IsSimpleMemberAccessExpression(memberAccess))
            {
                return false;
            }

            _syntaxFacts.GetPartsOfMemberAccessExpression(memberAccess, out var localInstance, out var memberName);
            _syntaxFacts.GetNameAndArityOfSimpleName(memberName, out var name, out var arity);

            if (arity != 0 || !name.Equals(nameof(IList.Add)))
            {
                return false;
            }

            instance = localInstance;
            return true;
        }
    }
}