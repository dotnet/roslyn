// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName;

internal static class ParenthesesTreeWriter
{
    public static string ToParenthesesFormat(SimpleTreeNode tree)
    {
        var sb = new StringBuilder();
        WriteNode(tree, sb);
        return sb.ToString();
    }

    private static void WriteNode(SimpleTreeNode node, StringBuilder sb)
    {
        sb.Append(node.Text);
        if (node is SimpleGroupNode group)
        {
            sb.Append('(');
            for (var i = 0; i < group.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                WriteNode(group[i], sb);
            }

            sb.Append(')');
        }
    }
}
