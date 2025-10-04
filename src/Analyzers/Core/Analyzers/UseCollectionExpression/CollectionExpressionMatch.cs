// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.UseCollectionExpression;

/// <summary>
/// Represents statements following an initializer that should be converted into collection-initializer/expression
/// elements.
/// </summary>
/// <param name="Node">The statement that follows that contains the values to add to the new collection-initializer or
/// collection-expression. Or the expression directly to add.</param>
/// <param name="UseSpread">Whether or not a spread (<c>.. x</c>) element should be created for this statement. This
/// is needed as the statement could be cases like <c>expr.Add(x)</c> vs. <c>expr.AddRange(x)</c>. This property
/// indicates that the latter should become a spread, without the consumer having to reexamine the statement to see
/// what form it is.</param>
/// <param name="UseCast">Whether we need to cast the expression in <paramref name="Node"/> to the appropriate element type.</param>
internal readonly record struct CollectionMatch<TMatchNode>(
    TMatchNode Node,
    bool UseSpread,
    bool UseCast = false) where TMatchNode : SyntaxNode;
