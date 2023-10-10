// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

/// <summary>
/// Helper class for working with C# syntax nodes.
/// </summary>
internal partial class CSharpSourceNode
{
    /// <summary>
    /// Extracts source nodes from a syntax node.
    /// </summary>
    /// <param name="rootNode">The root node.</param>
    /// <returns>A list of source nodes.</returns>
    public static ImmutableArray<CSharpSourceNode> ExtractSourceNodes(SyntaxNode rootNode)
    {
        using var _ = ArrayBuilder<CSharpSourceNode>.GetInstance(out var sourceNodes);
        var stack = new Stack<SyntaxNode>();
        stack.Push(rootNode);

        while (stack.Count > 0)
        {
            var currentNode = stack.Pop();
            var sourceNode = CreateSourceNode(currentNode);

            if (sourceNode is not null)
            {
                // Mixed scoped types nodes are unsupported
                if (sourceNodes.Count > 0 && sourceNodes[0].Scope != sourceNode.Scope)
                    return ImmutableArray<CSharpSourceNode>.Empty;

                sourceNodes.Add(sourceNode);
            }
            else
            {
                // Add child nodes to the stack in reverse order for depth-first search
                foreach (var childNode in currentNode.ChildNodes().Reverse())
                {
                    stack.Push(childNode);
                }
            }
        }

        return sourceNodes.ToImmutable();

        static CSharpSourceNode? CreateSourceNode(SyntaxNode syntaxNode)
        {
            if (IsScopedNode(syntaxNode, out var scope) || IsSimpleNode(syntaxNode))
            {
                if (scope is not Scope.None)
                {
                    var bodySyntax = GetBodySyntax(syntaxNode);
                    if (bodySyntax is BlockSyntax blockSyntax)
                    {
                        // Mark as invalid when any of the brackets is missing.
                        if (blockSyntax.CloseBraceToken.IsMissing || blockSyntax.OpenBraceToken.IsMissing)
                        {
                            return null;
                        }
                    }
                    else if (bodySyntax is StructDeclarationSyntax str)
                    {
                        if (str.CloseBraceToken.IsMissing || str.OpenBraceToken.IsMissing)
                        {
                            return null;
                        }
                    }
                    else if (bodySyntax is RecordDeclarationSyntax rec)
                    {
                        // Record may or may not have the brace tokens depending on how they are
                        // initialized.
                        // So we will only return false when we detect that they did have an open brace token
                        // but then they didn't have a close brace token;
                        if (!rec.OpenBraceToken.IsMissing && rec.CloseBraceToken.IsMissing)
                        {
                            return null;
                        }

                        // If record doesn't have neither close brace token or semi column
                        if (rec.CloseBraceToken.IsMissing && rec.SemicolonToken.IsMissing)
                        {
                            return null;
                        }
                    }
                    else if (bodySyntax is BaseTypeDeclarationSyntax typeSyntax)
                    {
                        if (typeSyntax.CloseBraceToken.IsMissing || typeSyntax.OpenBraceToken.IsMissing)
                        {
                            return null;
                        }
                    }
                }

                return new(syntaxNode, scope);
            }

            return null;
        }
    }

    /// <summary>
    /// Determines whether the node is a scoped node.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <param name="scope">The scope.</param>
    /// <returns>True if node is a scoped node, false otherwise.</returns>
    public static bool IsScopedNode(SyntaxNode node, out Scope scope)
        => IsScoped(node.Kind(), out scope);

    /// <summary>
    /// Determines whether the node is a simple node.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <returns>True if node is a simple node, false otherwise.</returns>
    public static bool IsSimpleNode(SyntaxNode node)
        => IsSimple(node.Kind());

    /// <summary>
    /// Gets the body syntax of the specified node.
    /// </summary>
    /// <param name="node">The node to get the body syntax of.</param>
    /// <returns>The body syntax of the specified node, if it has any.</returns>
    public static SyntaxNode? GetBodySyntax(SyntaxNode node)
    {
        return node switch
        {
            (MethodDeclarationSyntax method) => method.Body,
            (LocalFunctionStatementSyntax function) => function.Body,
            (ConstructorDeclarationSyntax constructor) => constructor.Body,
            (StructDeclarationSyntax st) => st,
            (RecordDeclarationSyntax rc) => rc,
            (BaseTypeDeclarationSyntax cl) => cl,
            _ => node.ChildNodes().FirstOrDefault(cn => cn is BlockSyntax),
        };
    }

    /// <summary>
    /// Gets the open brace token and closing brace token of a specified syntax node.
    /// </summary>
    /// <param name="node">The syntax node to retrieve the brace tokens from.</param>
    /// <returns>
    /// An object of type BraceTokens which contain the open brace token and closing brace token of a specified syntax node.
    /// Or null when the node doesn't have an open brace and close brace tokens.
    /// </returns>
    public static BraceTokens? GetOpenCloseBraceTokens(SyntaxNode node)
    {
        var bodySyntax = GetBodySyntax(node);
        if (bodySyntax is null)
        {
            return null;
        }

        if (bodySyntax is BlockSyntax bs)
        {
            return new(bs.OpenBraceToken, bs.CloseBraceToken);
        }

        if (bodySyntax is StructDeclarationSyntax sds)
        {
            return new(sds.OpenBraceToken, sds.CloseBraceToken);
        }

        if (bodySyntax is RecordDeclarationSyntax rds)
        {
            return new(rds.OpenBraceToken, rds.CloseBraceToken);
        }

        if (bodySyntax is BaseTypeDeclarationSyntax btds)
        {
            return new(btds.OpenBraceToken, btds.CloseBraceToken);
        }

        return null;
    }

    public record BraceTokens(SyntaxToken Open, SyntaxToken Close);
}
