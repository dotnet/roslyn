// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Analyzers.UseCollectionInitializer;

internal static class UseCollectionInitializerHelpers
{
    public const string UseCollectionExpressionName = nameof(UseCollectionExpressionName);

    public static readonly ImmutableDictionary<string, string?> UseCollectionExpressionProperties =
        ImmutableDictionary<string, string?>.Empty.Add(UseCollectionExpressionName, UseCollectionExpressionName);

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

            if (extractedChild is not TStatementSyntax childStatement)
                break;

            yield return childStatement;
        }
    }
}
