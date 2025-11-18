// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.UseCollectionExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

internal sealed partial class CSharpUseCollectionInitializerCodeFixProvider
{
    /// <summary>
    /// Creates the final collection-expression <c>[...]</c> that will replace the given <paramref
    /// name="objectCreation"/> expression.
    /// </summary>
    private static Task<CollectionExpressionSyntax> CreateCollectionExpressionAsync(
        Document document,
        BaseObjectCreationExpressionSyntax objectCreation,
        ImmutableArray<CollectionMatch<SyntaxNode>> preMatches,
        ImmutableArray<CollectionMatch<SyntaxNode>> postMatches,
        CancellationToken cancellationToken)
    {
        return CSharpCollectionExpressionRewriter.CreateCollectionExpressionAsync(
            document,
            objectCreation,
            preMatches,
            postMatches,
            static objectCreation => objectCreation.Initializer,
            static (objectCreation, initializer) => objectCreation.WithInitializer(initializer),
            cancellationToken);
    }
}
