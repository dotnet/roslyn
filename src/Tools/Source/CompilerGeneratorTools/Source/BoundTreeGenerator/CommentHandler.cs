// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using BoundTreeGenerator;

namespace BoundTreeGenerator
{
    internal static class CommentHandler
    {
        private static IEnumerable<XNode> GetPreviousNodes(XElement node)
        {
            XNode cur = node;
            while (true)
            {
                var p = cur.PreviousNode;
                if (p is { })
                {
                    yield return p;
                }
                else
                {
                    break;
                }
                cur = p;
            }
        }

        private static IEnumerable<string> GetPreviousComments(XElement element)
        {
            foreach (var node in GetPreviousNodes(element))
            {
                if (node is XElement)
                {
                    break;
                }
                if (node is XComment { Value: { Length: > 0 } commentText })
                {
                    yield return commentText;
                }
            }
        }

        public static void HandleElementComment(XElement element, object obj, int maxCommentCount = int.MaxValue, string multiCommentSeparator = "-----------\r\n")
        {
            if (obj is CommentedNode commentedNode)
            {
                StringBuilder? strBuilder = null;
                foreach (var commentText in GetPreviousComments(element).Reverse().Take(maxCommentCount))
                {
                    if (strBuilder == null)
                    {
                        strBuilder = new();
                    }
                    else
                    {
                        strBuilder.Append(multiCommentSeparator);
                    }
                    strBuilder.AppendLine(commentText);
                }
                if (strBuilder is { })
                {
                    commentedNode.Comment = strBuilder.ToString().Trim();
                }
            }
        }
    }
}
