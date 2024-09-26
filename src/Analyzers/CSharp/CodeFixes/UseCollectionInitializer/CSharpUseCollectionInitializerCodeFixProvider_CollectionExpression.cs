// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

internal partial class CSharpUseCollectionInitializerCodeFixProvider
{
    /// <summary>
    /// Creates the final collection-expression <c>[...]</c> that will replace the given <paramref
    /// name="objectCreation"/> expression.
    /// </summary>
    private static async Task<CollectionExpressionSyntax> CreateCollectionExpressionAsync(
        Document document,
        BaseObjectCreationExpressionSyntax objectCreation,
        ImmutableArray<Match<StatementSyntax>> matches,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        using var finalMatches = TemporaryArray<CollectionExpressionMatch<SyntaxNode>>.Empty;

        // If we have an argument to the constructor (that is not the 'capacity' argument), then include that as a
        // spreaded value to the final collection expression before all the other elements we're adding.
        if (objectCreation.ArgumentList?.Arguments is [{ Expression: var expression }])
        {
            var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (type?.SpecialType is not SpecialType.System_Int32)
                finalMatches.Add(new(expression, UseSpread: true));
        }

        finalMatches.AddRange(matches.SelectAsArray(m => new CollectionExpressionMatch<SyntaxNode>(m.Statement, m.UseSpread)));

        return await CSharpCollectionExpressionRewriter.CreateCollectionExpressionAsync(
            document,
            objectCreation,
            finalMatches.ToImmutableAndClear(),
            static objectCreation => objectCreation.Initializer,
            static (objectCreation, initializer) => objectCreation.WithInitializer(initializer),
            cancellationToken).ConfigureAwait(false);
    }
}
