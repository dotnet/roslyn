// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName
{
    internal static class ParenthesesTreeWriter
    {
        public static string ToParenthesesFormat(SimpleTreeNode tree)
        {
            if (tree == null)
            {
                return null;
            }

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
}
