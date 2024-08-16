// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Snippets;

/// <summary>
/// Information about inline expression for inline statement snippets
/// </summary>
internal sealed class InlineExpressionInfo(SyntaxNode node, TypeInfo typeInfo)
{
    /// <summary>
    /// Right-hand side of an accessing expression.
    /// Must be an expression node.
    /// Do NOT use it to obtain semantic info.
    /// If you need type information about this node use <see cref="TypeInfo" /> property
    /// </summary>
    public SyntaxNode Node { get; } = node;

    /// <summary>
    /// Type information of an accessing expression
    /// </summary>
    public TypeInfo TypeInfo { get; } = typeInfo;
}
