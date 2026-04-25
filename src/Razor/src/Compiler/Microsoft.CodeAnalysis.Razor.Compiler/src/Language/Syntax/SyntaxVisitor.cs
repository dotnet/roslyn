// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

/// <summary>
///  Represents a <see cref="RazorSyntaxNode"/> visitor that visits only the single
///  RazorSyntaxNode passed into its Visit method and produces a value of the type
///  specified by the <typeparamref name="TResult"/> parameter.
/// </summary>
/// <typeparam name="TResult">
///  The type of the return value this visitor's Visit method.
/// </typeparam>
internal abstract partial class SyntaxVisitor<TResult>
{
    public virtual TResult? Visit(SyntaxNode? node)
    {
        if (node != null)
        {
            Debug.Assert(!node.IsToken);
            Debug.Assert(!node.IsList);

            return ((RazorSyntaxNode)node).Accept(this);
        }

        return default;
    }

    protected virtual TResult? DefaultVisit(SyntaxNode node)
    {
        Debug.Assert(!node.IsToken);
        Debug.Assert(!node.IsList);

        return default;
    }
}

/// <summary>
///  Represents a <see cref="RazorSyntaxNode"/> visitor that visits only the single
///  RazorSyntaxNode passed into its Visit method.
/// </summary>
internal abstract partial class SyntaxVisitor
{
    public virtual void Visit(SyntaxNode? node)
    {
        if (node != null)
        {
            Debug.Assert(!node.IsToken);
            Debug.Assert(!node.IsList);

            ((RazorSyntaxNode)node).Accept(this);
        }
    }

    public virtual void DefaultVisit(SyntaxNode node)
    {
        Debug.Assert(!node.IsToken);
        Debug.Assert(!node.IsList);
    }
}
