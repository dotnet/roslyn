// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal readonly struct Argument<TExpressionSyntax>(RefKind refKind, string? name, TExpressionSyntax? expression)
    where TExpressionSyntax : SyntaxNode
{
    public readonly RefKind RefKind = refKind;
    public readonly string Name = name ?? "";
    public readonly TExpressionSyntax? Expression = expression;

    public bool IsNamed => !string.IsNullOrEmpty(Name);
}
