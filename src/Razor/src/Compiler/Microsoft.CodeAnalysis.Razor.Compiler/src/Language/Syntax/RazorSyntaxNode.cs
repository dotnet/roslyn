// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class RazorSyntaxNode : SyntaxNode
{
    public RazorSyntaxNode(GreenNode green, SyntaxNode parent, int position)
        : base(green, parent, position)
    {
    }

    // For debugging
#pragma warning disable IDE0051 // Remove unused private members
    private string SerializedValue => SyntaxSerializer.Default.Serialize(this);
#pragma warning restore IDE0051 // Remove unused private members

    public abstract TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor);

    public abstract void Accept(SyntaxVisitor visitor);

    protected internal override SyntaxNode ReplaceCore<TNode>(
        IEnumerable<TNode>? nodes = null,
        Func<TNode, TNode, SyntaxNode>? computeReplacementNode = null,
        IEnumerable<SyntaxToken>? tokens = null,
        Func<SyntaxToken, SyntaxToken, SyntaxToken>? computeReplacementToken = null)
        => SyntaxReplacer.Replace(this, nodes, computeReplacementNode, tokens, computeReplacementToken);

    protected internal override SyntaxNode ReplaceNodeInListCore(SyntaxNode originalNode, IEnumerable<SyntaxNode> replacementNodes)
        => SyntaxReplacer.ReplaceNodeInList(this, originalNode, replacementNodes);

    protected internal override SyntaxNode InsertNodesInListCore(SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore)
        => SyntaxReplacer.InsertNodeInList(this, nodeInList, nodesToInsert, insertBefore);

    protected internal override SyntaxNode ReplaceTokenInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens)
        => SyntaxReplacer.ReplaceTokenInList(this, originalToken, newTokens);

    protected internal override SyntaxNode InsertTokensInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens, bool insertBefore)
        => SyntaxReplacer.InsertTokenInList(this, originalToken, newTokens, insertBefore);
}
