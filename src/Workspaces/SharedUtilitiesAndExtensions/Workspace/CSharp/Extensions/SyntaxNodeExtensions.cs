// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static partial class SyntaxNodeExtensions
{
    public static SyntaxNode WithPrependedNonIndentationTriviaFrom(this SyntaxNode to, SyntaxNode from)
        => SyntaxNodeOrTokenExtensions.WithPrependedNonIndentationTriviaFrom((SyntaxNodeOrToken)to, (SyntaxNodeOrToken)from).AsNode()!;
}
