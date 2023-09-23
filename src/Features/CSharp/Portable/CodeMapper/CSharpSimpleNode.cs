// Copyright (c) Microsoft Corporation. All rights reserved.
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

/// <summary>
/// Represents a simple syntax node in a C# source file.
/// A simple node can be represented as any node that doesn't have
/// a scope. It can be a declaration field, an event, delegate, etc.
/// </summary>
internal class CSharpSimpleNode : CSharpSourceNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpSimpleNode"/> class.
    /// </summary>
    /// <param name="node">The syntax node to wrap.</param>
    public CSharpSimpleNode(SyntaxNode node)
        : base(node)
    {
    }
}
