// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

/// <inheritdoc cref="Match"/>
internal readonly record struct CollectionExpressionMatch<TMatchNode>(
    TMatchNode Node,
    bool UseSpread) where TMatchNode : SyntaxNode;
