// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
            char ex= '\0';
            char ac= '\0';

            if (textSpanOpt == null)
            {
                index = 0;
            }
            else
            {
                var fromText = text.ToString(textSpanOpt.Value);
                var fromNode = node.ToFullString();
                (index,ex, ac) = FindFirstDifference(fromText, fromNode);
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
                    message = string.Format("Unexpected difference (expected:{4} actual:{5}) at offset {0}: Line {1}, Column {2} \"{3}\"",
                        index,
                        position.Line + 1,
                        position.Character + 1,
                        line.ToString(),ex,ac);
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
        private static (int index,char expected,char actual) FindFirstDifference(string s1, string s2)
        {
            var n1 = s1.Length;
            var n2 = s2.Length;
            var n = Math.Min(n1, n2);
            for (int i = 0; i < n; i++)
            {
                if (s1[i] != s2[i])
                {
                    return (i,s1[i],s2[i]);
                }
            }
            if (n1 == n2) return (index:-1,expected: '\0', actual: '\0');
            return (n + 1,'\0', '\0');
        }

        /// <summary>
        /// Returns <c>true</c> if the provided position is in a hidden region inaccessible to the user.
        /// </summary>
        public static bool IsHiddenPosition(this SyntaxTree tree, int position, CancellationToken cancellationToken = default)
        {
            if (!tree.HasHiddenRegions())
            {
                return false;
            }

            var lineVisibility = tree.GetLineVisibility(position, cancellationToken);
            return lineVisibility == LineVisibility.Hidden || lineVisibility == LineVisibility.BeforeFirstLineDirective;
        }
    }
}
