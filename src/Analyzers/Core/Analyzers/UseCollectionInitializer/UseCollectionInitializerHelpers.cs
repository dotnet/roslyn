// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer;

internal static class UseCollectionInitializerHelpers
{
    public const string UseCollectionExpressionName = nameof(UseCollectionExpressionName);

    public static readonly ImmutableDictionary<string, string?> UseCollectionExpressionProperties =
        ImmutableDictionary<string, string?>.Empty.Add(UseCollectionExpressionName, UseCollectionExpressionName);

    public static ImmutableArray<Location> GetLocationsToFade<TStatementSyntax>(
        ISyntaxFacts syntaxFacts,
        Match<TStatementSyntax> matchInfo)
        where TStatementSyntax : SyntaxNode
    {
        var match = matchInfo.Statement;
        var syntaxTree = match.SyntaxTree;
        if (syntaxFacts.IsExpressionStatement(match))
        {
            var expression = syntaxFacts.GetExpressionOfExpressionStatement(match);

            if (syntaxFacts.IsInvocationExpression(expression))
            {
                var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(expression);
                var additionalUnnecessaryLocations = ImmutableArray.Create(
                    syntaxTree.GetLocation(TextSpan.FromBounds(match.SpanStart, arguments[0].SpanStart)),
                    syntaxTree.GetLocation(TextSpan.FromBounds(arguments.Last().FullSpan.End, match.Span.End)));

                return additionalUnnecessaryLocations;
            }
        }
        else if (syntaxFacts.IsForEachStatement(match))
        {
            // For a `foreach (var x in expr) ...` statement, fade out the parts before and after `expr`.

            var expression = syntaxFacts.GetExpressionOfForeachStatement(match);
            var additionalUnnecessaryLocations = ImmutableArray.Create(
                syntaxTree.GetLocation(TextSpan.FromBounds(match.SpanStart, expression.SpanStart)),
                syntaxTree.GetLocation(TextSpan.FromBounds(expression.FullSpan.End, match.Span.End)));

            return additionalUnnecessaryLocations;
        }

        return ImmutableArray<Location>.Empty;
    }

    public static IEnumerable<TStatementSyntax> GetSubsequentStatements<TStatementSyntax>(
        ISyntaxFacts syntaxFacts,
        TStatementSyntax initialStatement) where TStatementSyntax : SyntaxNode
    {
        // If containing statement is inside a block (e.g. method), than we need to iterate through its child
        // statements. If containing statement is in top-level code, than we need to iterate through child statements of
        // containing compilation unit.
        var containingBlockOrCompilationUnit = initialStatement.GetRequiredParent();

        // In case of top-level code parent of the statement will be GlobalStatementSyntax, so we need to get its parent
        // in order to get CompilationUnitSyntax
        if (syntaxFacts.IsGlobalStatement(containingBlockOrCompilationUnit))
            containingBlockOrCompilationUnit = containingBlockOrCompilationUnit.Parent!;

        var foundStatement = false;
        foreach (var child in containingBlockOrCompilationUnit.ChildNodesAndTokens())
        {
            if (child.IsToken)
                continue;

            var childNode = child.AsNode()!;
            var extractedChild = syntaxFacts.IsGlobalStatement(childNode)
                ? syntaxFacts.GetStatementOfGlobalStatement(childNode)
                : childNode;

            if (!foundStatement)
            {
                if (extractedChild == initialStatement)
                {
                    foundStatement = true;
                }

                continue;
            }

            yield return (TStatementSyntax)extractedChild;
        }
    }

    public static bool TryAnalyzeInvocation(
        ISyntaxFacts syntaxFacts,
        TExpressionStatementSyntax statement,
        string addName,
        string? requiredArgumentName,
        bool forCollectionExpression,
        [NotNullWhen(true)] out TExpressionSyntax? instance)
    {
        instance = null;

        var invocationExpression = syntaxFacts.GetExpressionOfExpressionStatement(statement);
        if (!syntaxFacts.IsInvocationExpression(invocationExpression))
            return false;

        var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
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
            if (!syntaxFacts.IsSimpleArgument(argument))
                return false;

            var argumentExpression = syntaxFacts.GetExpressionOfArgument(argument);
            if (ExpressionContainsValuePatternOrReferencesInitializedSymbol(argumentExpression))
                return false;

            // VB allows for a collection initializer to be an argument.  i.e. `Goo({a, b, c})`.  This argument
            // cannot be used in an outer collection initializer as it would change meaning.  i.e.:
            //
            //      new List(Of IEnumerable(Of String)) { { a, b, c } }
            //
            // is not legal.  That's because instead of adding `{ a, b, c }` as a single element to the list, VB
            // instead looks for an 3-argument `Add` method to invoke on `List<T>` (which clearly fails).
            if (syntaxFacts.SyntaxKinds.CollectionInitializerExpression == argumentExpression.RawKind)
                return false;

            // If the caller is requiring a particular argument name, then validate that is what this argument
            // is referencing.
            if (requiredArgumentName != null)
            {
                if (!syntaxFacts.IsIdentifierName(argumentExpression))
                    return false;

                syntaxFacts.GetNameAndArityOfSimpleName(argumentExpression, out var suppliedName, out _);
                if (requiredArgumentName != suppliedName)
                    return false;
            }
        }

        var memberAccess = syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
        if (!syntaxFacts.IsSimpleMemberAccessExpression(memberAccess))
            return false;

        syntaxFacts.GetPartsOfMemberAccessExpression(memberAccess, out var localInstance, out var memberName);
        syntaxFacts.GetNameAndArityOfSimpleName(memberName, out var name, out var arity);

        if (arity != 0 || !Equals(name, addName))
            return false;

        instance = localInstance as TExpressionSyntax;
        return instance != null;
    }
}
