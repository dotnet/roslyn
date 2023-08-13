// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    internal readonly struct UpdateObjectCreationState<
        TExpressionSyntax,
        TStatementSyntax>
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
    {
        public readonly SemanticModel SemanticModel;
        public readonly ISyntaxFacts SyntaxFacts;
        public readonly TExpressionSyntax StartExpression;
        public readonly TStatementSyntax ContainingStatement;

        public readonly SyntaxNodeOrToken ValuePattern;
        public readonly ISymbol? InitializedSymbol;

        public UpdateObjectCreationState(
            SemanticModel semanticModel,
            ISyntaxFacts syntaxFacts,
            TExpressionSyntax startExpression,
            SyntaxNodeOrToken valuePattern,
            ISymbol? initializedSymbol)
        {
            SemanticModel = semanticModel;
            SyntaxFacts = syntaxFacts;
            StartExpression = startExpression;
            ContainingStatement = startExpression.FirstAncestorOrSelf<TStatementSyntax>()!;
            ValuePattern = valuePattern;
            InitializedSymbol = initializedSymbol;
        }

        public IEnumerable<TStatementSyntax> GetSubsequentStatements()
            => UseCollectionInitializerHelpers.GetSubsequentStatements(SyntaxFacts, ContainingStatement);

        public bool ValuePatternMatches(TExpressionSyntax expression)
        {
            if (ValuePattern.IsToken)
            {
                return SyntaxFacts.IsIdentifierName(expression) &&
                    SyntaxFacts.AreEquivalent(
                        ValuePattern.AsToken(),
                        SyntaxFacts.GetIdentifierOfSimpleName(expression));
            }
            else
            {
                return SyntaxFacts.AreEquivalent(
                    ValuePattern.AsNode(), expression);
            }
        }

        public bool ExpressionContainsValuePatternOrReferencesInitializedSymbol(
            SyntaxNode expression,
            CancellationToken cancellationToken)
        {
            foreach (var subExpression in expression.DescendantNodesAndSelf().OfType<TExpressionSyntax>())
            {
                if (!SyntaxFacts.IsNameOfSimpleMemberAccessExpression(subExpression) &&
                    !SyntaxFacts.IsNameOfMemberBindingExpression(subExpression))
                {
                    if (ValuePatternMatches(subExpression))
                        return true;
                }

                if (InitializedSymbol != null &&
                    InitializedSymbol.Equals(
                        SemanticModel.GetSymbolInfo(subExpression, cancellationToken).GetAnySymbol()))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAnalyzeInvocation<TExpressionStatementSyntax>(
            TExpressionStatementSyntax statement,
            string addName,
            string? requiredArgumentName,
            bool forCollectionExpression,
            CancellationToken cancellationToken,
            [NotNullWhen(true)] out TExpressionSyntax? instance)
            where TExpressionStatementSyntax : TStatementSyntax
        {
            instance = null;

            var invocationExpression = this.SyntaxFacts.GetExpressionOfExpressionStatement(statement);
            if (!this.SyntaxFacts.IsInvocationExpression(invocationExpression))
                return false;

            var arguments = this.SyntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
            if (arguments.Count < 1)
                return false;

            // Collection expressions can only call the single argument Add/AddRange methods on a type.
            // So if we don't have exactly one argument, fail out.
            if (forCollectionExpression && arguments.Count != 1)
                return false;

            if (requiredArgumentName != null && arguments.Count != 1)
                return false;

            foreach (var argument in arguments)
            {
                if (!this.SyntaxFacts.IsSimpleArgument(argument))
                    return false;

                var argumentExpression = this.SyntaxFacts.GetExpressionOfArgument(argument);
                if (ExpressionContainsValuePatternOrReferencesInitializedSymbol(argumentExpression, cancellationToken))
                    return false;

                // VB allows for a collection initializer to be an argument.  i.e. `Goo({a, b, c})`.  This argument
                // cannot be used in an outer collection initializer as it would change meaning.  i.e.:
                //
                //      new List(Of IEnumerable(Of String)) { { a, b, c } }
                //
                // is not legal.  That's because instead of adding `{ a, b, c }` as a single element to the list, VB
                // instead looks for an 3-argument `Add` method to invoke on `List<T>` (which clearly fails).
                if (this.SyntaxFacts.SyntaxKinds.CollectionInitializerExpression == argumentExpression.RawKind)
                    return false;

                // If the caller is requiring a particular argument name, then validate that is what this argument
                // is referencing.
                if (requiredArgumentName != null)
                {
                    if (!this.SyntaxFacts.IsIdentifierName(argumentExpression))
                        return false;

                    this.SyntaxFacts.GetNameAndArityOfSimpleName(argumentExpression, out var suppliedName, out _);
                    if (requiredArgumentName != suppliedName)
                        return false;
                }
            }

            var memberAccess = this.SyntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
            if (!this.SyntaxFacts.IsSimpleMemberAccessExpression(memberAccess))
                return false;

            this.SyntaxFacts.GetPartsOfMemberAccessExpression(memberAccess, out var localInstance, out var memberName);
            this.SyntaxFacts.GetNameAndArityOfSimpleName(memberName, out var name, out var arity);

            if (arity != 0 || !Equals(name, addName))
                return false;

            instance = localInstance as TExpressionSyntax;
            return instance != null;
        }
    }

    internal static class UpdateObjectCreationHelpers
    {
        public static UpdateObjectCreationState<TExpressionSyntax, TStatementSyntax>? TryInitializeVariableDeclarationCase<TExpressionSyntax, TStatementSyntax>(
            SemanticModel semanticModel,
            ISyntaxFacts syntaxFacts,
            TExpressionSyntax rootExpression,
            TStatementSyntax containingStatement,
            CancellationToken cancellationToken)
            where TExpressionSyntax : SyntaxNode
            where TStatementSyntax : SyntaxNode
        {
            if (!syntaxFacts.IsLocalDeclarationStatement(containingStatement))
                return null;

            var containingDeclarator = rootExpression.Parent?.Parent;
            if (containingDeclarator is null)
                return null;

            var initializedSymbol = semanticModel.GetDeclaredSymbol(containingDeclarator, cancellationToken);
            if (initializedSymbol is ILocalSymbol local &&
                local.Type is IDynamicTypeSymbol)
            {
                // Not supported if we're creating a dynamic local.  The object we're instantiating
                // may not have the members that we're trying to access on the dynamic object.
                return null;
            }

            if (!syntaxFacts.IsDeclaratorOfLocalDeclarationStatement(containingDeclarator, containingStatement))
                return null;

            var valuePattern = syntaxFacts.GetIdentifierOfVariableDeclarator(containingDeclarator);
            return new(semanticModel, syntaxFacts, rootExpression, valuePattern, initializedSymbol);
        }

        public static UpdateObjectCreationState<TExpressionSyntax, TStatementSyntax>? TryInitializeAssignmentCase<TExpressionSyntax, TStatementSyntax>(
            SemanticModel semanticModel,
            ISyntaxFacts syntaxFacts,
            TExpressionSyntax rootExpression,
            TStatementSyntax containingStatement,
            CancellationToken cancellationToken)
            where TExpressionSyntax : SyntaxNode
            where TStatementSyntax : SyntaxNode
        {
            if (!syntaxFacts.IsSimpleAssignmentStatement(containingStatement))
                return null;

            syntaxFacts.GetPartsOfAssignmentStatement(containingStatement,
                out var left, out var right);
            if (right != rootExpression)
                return null;

            var typeInfo = semanticModel.GetTypeInfo(left, cancellationToken);
            if (typeInfo.Type is IDynamicTypeSymbol || typeInfo.ConvertedType is IDynamicTypeSymbol)
            {
                // Not supported if we're initializing something dynamic.  The object we're instantiating
                // may not have the members that we're trying to access on the dynamic object.
                return null;
            }

            var initializedSymbol = semanticModel.GetSymbolInfo(left, cancellationToken).GetAnySymbol();
            return new(semanticModel, syntaxFacts, rootExpression, left, initializedSymbol);
        }
    }
}
