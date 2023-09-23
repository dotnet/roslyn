// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

/// <summary>
/// Helper class for working with C# syntax nodes.
/// </summary>
internal static class NodeHelper
{
    /// <summary>
    /// Extracts source nodes from a syntax node.
    /// </summary>
    /// <param name="rootNode">The root node.</param>
    /// <returns>A list of source nodes.</returns>
    public static IList<CSharpSourceNode> ExtractSourceNodes(SyntaxNode rootNode)
    {
        var sourceNodes = new List<CSharpSourceNode>();
        var stack = new Stack<SyntaxNode>();
        stack.Push(rootNode);

        while (stack.Count > 0)
        {
            var currentNode = stack.Pop();

            if (IsScopedNode(currentNode, out var scope) || IsSimpleNode(currentNode))
            {
                CSharpSourceNode sourceNode;
                if (scope != Scope.Unknown)
                {
                    sourceNode = new CSharpScopedNode(currentNode, scope);
                }
                else
                {
                    sourceNode = new CSharpSimpleNode(currentNode);
                }

                if (sourceNode.IsValid())
                {
                    sourceNodes.Add(sourceNode);
                }

                continue;
            }

            // Add child nodes to the stack in reverse order for depth-first search
            foreach (var childNode in currentNode.ChildNodes().Reverse())
            {
                stack.Push(childNode);
            }
        }

        return sourceNodes;
    }

    /// <summary>
    /// Gets the syntax node asynchronously.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="cancellation">The cancellation token.</param>
    /// <returns>The syntax node.</returns>
    public static async Task<SyntaxNode?> GetSyntaxAsync(string code, CancellationToken cancellation)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        if (tree is null)
        {
            return null;
        }

        return await tree.GetRootAsync(cancellation);
    }

    /// <summary>
    /// Determines whether the node is a scoped node.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <param name="scope">The scope.</param>
    /// <returns>True if node is a scoped node, false otherwise.</returns>
    public static bool IsScopedNode(SyntaxNode node, out Scope scope)
    {
        scope = Scope.Unknown;
        var nodeType = node.GetType();
        if (NodeTypes.Exclude.Contains(nodeType))
        {
            return false;
        }

        foreach (var scopeTypes in NodeTypes.Scoped)
        {
            foreach (var type in scopeTypes.Value)
            {
                if (nodeType == type)
                {
                    scope = scopeTypes.Key;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the node is a simple node.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <returns>True if node is a simple node, false otherwise.</returns>
    public static bool IsSimpleNode(SyntaxNode node)
    {
        var nodeType = node.GetType();
        if (NodeTypes.Exclude.Contains(nodeType))
        {
            return false;
        }

        foreach (var type in NodeTypes.Simple)
        {
            if (node.GetType() == type)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates if the given code contains any node that we identify as invalid.
    /// If the code cannot be mapped into any of our supported nodes, a true will be returned as default.
    /// We rather add the exceptions later on, rather than missing the opportunity of inserting something that looks good but we haven't added yet.
    /// </summary>
    /// <param name="code">The code to evaluate.</param>
    /// <param name="cancellation">A cancellation token.</param>
    /// <returns>Returns a value indicating whether an InsertAtLocation button can be created for the provided code.</returns>
    public static async Task<bool> IsValidInsertionCodeAsync(string code, CancellationToken cancellation)
    {
        var node = CSharpSyntaxTree.ParseText(code);
        var root = await node.GetRootAsync(cancellation);
        var stack = new Stack<SyntaxNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var currentNode = stack.Pop();

            if (IsScopedNode(currentNode, out var scope) || IsSimpleNode(currentNode))
            {
                CSharpSourceNode sourceNode;
                if (scope != Scope.Unknown)
                {
                    sourceNode = new CSharpScopedNode(currentNode, scope);
                }
                else
                {
                    sourceNode = new CSharpSimpleNode(currentNode);
                }

                if (!sourceNode.IsValid())
                {
                    // will only return false when we find a node that we support
                    // and we determine that it is currently on an invalid state.
                    return false;
                }

                continue;
            }

            // Add child nodes to the stack in reverse order for depth-first search
            foreach (var childNode in currentNode.ChildNodes().Reverse())
            {
                stack.Push(childNode);
            }
        }

        return true;
    }

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
