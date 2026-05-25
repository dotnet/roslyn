// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionExpression;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseInitializer;

internal sealed partial class CSharpUseInitializerCodeFixProvider
{
    /// <summary>
    /// Creates the final collection-expression <c>[...]</c> that will replace the given <paramref
    /// name="objectCreation"/> expression.
    /// </summary>
    private static Task<CollectionExpressionSyntax> CreateCollectionExpressionAsync(
        Document document,
        BaseObjectCreationExpressionSyntax objectCreation,
        ImmutableArray<InitializerMatch<SyntaxNode>> preMatches,
        ImmutableArray<InitializerMatch<SyntaxNode>> postMatches,
        CancellationToken cancellationToken)
    {
        // The collection-expression rewriter (shared with the IDE0300+ family) still consumes
        // the legacy `CollectionMatch<TMatchNode>` shape — those analyzers are outside the
        // IDE0017+IDE0028 unification's scope. Translate at this boundary; the field mapping
        // is direct (`Node`/`UseSpread`/`UseCast`/`UseKeyValue` are preserved verbatim) and
        // the discriminator carried by `InitializerMatch.Kind` is unused by the rewriter,
        // which already inspects the node's syntax shape to decide how to emit.
        return CSharpCollectionExpressionRewriter.CreateCollectionExpressionAsync(
            document,
            objectCreation,
            preMatches.SelectAsArray(ToCollectionMatch),
            postMatches.SelectAsArray(ToCollectionMatch),
            static objectCreation => objectCreation.Initializer,
            static (objectCreation, initializer) => objectCreation.WithInitializer(initializer),
            cancellationToken);

        static CollectionMatch<SyntaxNode> ToCollectionMatch(InitializerMatch<SyntaxNode> match)
            => new(match.Node, match.UseSpread, match.UseCast, match.UseKeyValue);
    }
}
