// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias Baseline;
using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using SyntaxNode0 = Baseline::Microsoft.CodeAnalysis.SyntaxNode;

namespace Roslyn.SyntaxRewriterBenchmark;

internal static class SyntaxNodeExtensions
{
    /// <summary>
    ///  <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.Blender"/>'s Cursor.IndexOfNodeInParent
    /// </summary>
    internal static int BaselineIndexOfNodeInParent(this SyntaxNode node)
    {
        if (node.Parent == null)
        {
            return 0;
        }

        var children = node.Parent.ChildNodesAndTokens();
        var index = SyntaxNodeOrToken.GetFirstChildIndexSpanningPosition(children, node.Position);
        for (int i = index, n = children.Count; i < n; i++)
        {
            var child = children[i];
            if (child == node)
            {
                return i;
            }
        }

        throw ExceptionUtilities.Unreachable();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static int IndexOfNodeInParent(this SyntaxNode node)
    {
        // Note: if the syntax node is from the baseline assembly, virtual calls will still point to methods there.

        if (node.Parent == null)
        {
            return 0;
        }

        var index = node.Parent.GetIndexOfChild(node);
        if (index != -1)
        {
            return index;
        }
        else
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    internal static int BaselineIndexOfNodeInParent(this SyntaxNode0 node) => Unsafe.As<SyntaxNode>(node).BaselineIndexOfNodeInParent();

    [Obsolete("May not perform as expected due to type checks.")]
    internal static int IndexOfNodeInParent(this SyntaxNode0 node) => Unsafe.As<SyntaxNode>(node).IndexOfNodeInParent();

}
