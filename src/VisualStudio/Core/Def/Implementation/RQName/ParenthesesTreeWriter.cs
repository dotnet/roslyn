// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.Implementation.RQName.SimpleTree;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.RQName
{
    internal static class ParenthesesTreeWriter
    {
        public static string ToParenthesesFormat(SimpleTreeNode tree)
        {
            StringBuilder sb = new StringBuilder();
            WriteNode(tree, sb);
            return sb.ToString();
        }

        private static void WriteNode(SimpleTreeNode node, StringBuilder sb)
        {
            sb.Append(node.Text);
            if (node is SimpleGroupNode)
            {
                SimpleGroupNode group = (SimpleGroupNode)node;
                sb.Append('(');
                for (int i = 0; i < group.Count; i++)
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
