// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal static class SyntaxTreeExtensions
    {
        /// <summary>
        /// Verify nodes match source.
        /// </summary>
        [Conditional("DEBUG")]
        internal static void VerifySource(this SyntaxTree tree, IEnumerable<TextChangeRange> changes = null)
        {
            var root = tree.GetRoot();
            var text = tree.GetText();
            var fullSpan = new TextSpan(0, text.Length);
            SyntaxNode node = null;

            // If only a subset of the document has changed,
            // just check that subset to reduce verification cost.
            if (changes != null)
            {
                var change = TextChangeRange.Collapse(changes).Span;
                if (change != fullSpan)
                {
                    // Find the lowest node in the tree that contains the changed region.
                    node = root.DescendantNodes(n => n.FullSpan.Contains(change)).LastOrDefault();
                }
            }

            if (node == null)
            {
                node = root;
            }

            var span = node.FullSpan;
            var textSpanOpt = span.Intersection(fullSpan);
            int index;

            if (textSpanOpt == null)
            {
                index = 0;
            }
            else
            {
                var fromText = text.ToString(textSpanOpt.Value);
                var fromNode = node.ToFullString();
                index = FindFirstDifference(fromText, fromNode);
            }

            if (index >= 0)
            {
                index += span.Start;
                string message;
                if (index < text.Length)
                {
                    var position = text.Lines.GetLinePosition(index);
                    var line = text.Lines[position.Line];
                    var allText = text.ToString(); // Entire document as string to allow inspecting the text in the debugger.
                    message = string.Format("Unexpected difference at offset {0}: Line {1}, Column {2} \"{3}\"",
                        index,
                        position.Line + 1,
                        position.Character + 1,
                        line.ToString());
                }
                else
                {
                    message = "Unexpected difference past end of the file";
                }
                Debug.Assert(false, message);
            }
        }

        /// <summary>
        /// Return the index of the first difference between
        /// the two strings, or -1 if the strings are the same.
        /// </summary>
        private static int FindFirstDifference(string s1, string s2)
        {
            var n1 = s1.Length;
            var n2 = s2.Length;
            var n = Math.Min(n1, n2);
            for (int i = 0; i < n; i++)
            {
                if (s1[i] != s2[i])
                {
                    return i;
                }
            }
            return (n1 == n2) ? -1 : n + 1;
        }
    }
}
