// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    internal static class BaseSyntaxExtensions
    {
        internal static InternalSyntax.SyntaxNode ToGreen(this IBaseSyntaxNodeExt node)
        {
            var green = node as InternalSyntax.SyntaxNode;
            if (green == null && node != null)
            {
                green = ((SyntaxNode)node).Green;
            }

            return green;
        }

        internal static InternalSyntax.SyntaxList<T> ToGreenList<T>(this IBaseSyntaxNodeExt node) where T : InternalSyntax.SyntaxNode
        {
            return new InternalSyntax.SyntaxList<T>(node.ToGreen());
        }

        internal static InternalSyntax.SeparatedSyntaxList<T> ToGreenSeparatedList<T>(this IBaseSyntaxNodeExt node) where T : InternalSyntax.SyntaxNode
        {
            return new InternalSyntax.SeparatedSyntaxList<T>(new InternalSyntax.SyntaxList<InternalSyntax.SyntaxNode>(node.ToGreen()));
        }

        internal static SyntaxNode ToRed(this IBaseSyntaxNodeExt node)
        {
            var red = node as SyntaxNode;
            if (red == null && node != null)
            {
                red = ((InternalSyntax.SyntaxNode)node).ToRed(null);
            }

            return red;
        }
    }
}