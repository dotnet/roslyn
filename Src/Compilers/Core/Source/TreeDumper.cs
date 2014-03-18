// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // These classes are for debug and testing purposes only. It is frequently handy to be 
    // able to create a string representation of a complex tree-based data type. The idea
    // here is to first transform your tree into a standard "tree dumper node" tree, where
    // each node in the tree has a name, some optional data, and a sequence of child nodes.
    // Once in a standard format the tree can then be rendered in a variety of ways 
    // depending on what is most useful to you.
    //
    // I've started with two string formats. First, a "compact" format in which there is
    // exactly one line per node of the tree:
    //
    //   root
    //   ├─a1
    //   │ └─a1b1
    //   ├─a2
    //   │ ├─a2b1
    //   │ │ └─a2b1c1
    //   │ └─a2b2
    //   │   ├─a2b2c1
    //   │   │ └─a2b2c1d1
    //   │   └─a2b2c2
    //   └─a3
    //     └─a3b1
    //
    // And second, an XML format:
    //
    // <root>
    // <children>
    // <child>
    // value1
    // </child>
    // <child>
    // value2
    // </child>
    // </children>
    // </root>
    //
    // The XML format is much more verbose, but handy if you want to then pipe it into an XML tree viewer.

    /// <summary>
    /// This is ONLY used id BoundNode.cs Debug method - Dump()
    /// </summary>
    internal sealed class TreeDumper
    {
        private readonly StringBuilder sb;

        private TreeDumper()
        {
            this.sb = new StringBuilder();
        }

        public static string DumpCompact(TreeDumperNode root)
        {
            var dumper = new TreeDumper();
            dumper.DoDumpCompact(root, string.Empty);
            return dumper.sb.ToString();
        }

        private void DoDumpCompact(TreeDumperNode node, string indent)
        {
            Debug.Assert(node != null);
            Debug.Assert(indent != null);

            // Precondition: indentation and prefix has already been output
            // Precondition: indent is correct for node's *children*
            sb.Append(node.Text);
            if (node.Value != null)
            {
                sb.AppendFormat(": {0}", DumperString(node.Value));
            }

            sb.AppendLine();
            var children = node.Children.ToList();
            for (int i = 0; i < children.Count; ++i)
            {
                var child = children[i];
                if (child == null)
                {
                    continue;
                }

                sb.Append(indent);
                sb.Append(i == children.Count - 1 ? '└' : '├');
                sb.Append('─');

                // First precondition met; now work out the string needed to indent 
                // the child node's children:
                DoDumpCompact(child, indent + (i == children.Count - 1 ? "  " : "│ "));
            }
        }

        public static string DumpXML(TreeDumperNode root, string indent = null)
        {
            var dumper = new TreeDumper();
            dumper.DoDumpXML(root, string.Empty, string.IsNullOrEmpty(indent) ? string.Empty : indent);
            return dumper.sb.ToString();
        }

        private void DoDumpXML(TreeDumperNode node, string indent, string relativeIndent)
        {
            Debug.Assert(node != null);
            if (!node.Children.Any(child => child != null))
            {
                sb.Append(indent);
                if (node.Value != null)
                {
                    sb.AppendFormat("<{0}>{1}</{0}>", node.Text, DumperString(node.Value));
                }
                else
                {
                    sb.AppendFormat("<{0} />", node.Text);
                }
                sb.AppendLine();
            }
            else
            {
                sb.Append(indent);
                sb.AppendFormat("<{0}>", node.Text);
                sb.AppendLine();
                if (node.Value != null)
                {
                    sb.Append(indent);
                    sb.AppendFormat("{0}", DumperString(node.Value));
                    sb.AppendLine();
                }

                var childIndent = indent + relativeIndent;
                foreach (var child in node.Children)
                {
                    if (child == null)
                    {
                        continue;
                    }

                    DoDumpXML(child, childIndent, relativeIndent);
                }

                sb.Append(indent);
                sb.AppendFormat("</{0}>", node.Text);
                sb.AppendLine();
            }
        }

        // an (awful) test for a null read-only-array.  Is there no better way to do this?
        private static bool IsDefaultImmutableArray(Object o)
        {
            var ti = o.GetType().GetTypeInfo();
            return ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(ImmutableArray<>) && (bool)ti.GetDeclaredMethod("get_IsDefault").Invoke(o, new object[0]);
        }

        private string DumperString(object o)
        {
            string result;

            if (o == null)
            {
                result = "(null)";
            }
            else if (o is string)
            {
                result = (string)o;
            }
            else if (IsDefaultImmutableArray(o))
            {
                result = "(null)";
            }
            else if (o is IEnumerable)
            {
                IEnumerable seq = (IEnumerable)o;
                result = string.Format("{{{0}}}", string.Join(", ", seq.Cast<object>().Select(DumperString).ToArray()));
            }
            else if (o is ISymbol)
            {
                result = ((ISymbol)o).ToDisplayString(SymbolDisplayFormat.TestFormat);
            }
            else
            {
                result = o.ToString();
            }

            return result;
        }
    }

    /// <summary>
    /// This is ONLY used for debugging purpose
    /// </summary>
    internal sealed class TreeDumperNode
    {
        public TreeDumperNode(string text, object value, IEnumerable<TreeDumperNode> children)
        {
            this.Text = text;
            this.Value = value;
            this.Children = children ?? SpecializedCollections.EmptyEnumerable<TreeDumperNode>();
        }

        public TreeDumperNode(string text) : this(text, null, null) { }
        public object Value { get; private set; }
        public string Text { get; private set; }
        public IEnumerable<TreeDumperNode> Children { get; private set; }
        public TreeDumperNode this[string child]
        {
            get
            {
                return Children.Where(c=>c.Text == child).FirstOrDefault();
            }
        }

        // enumerates all edges of the tree yielding (parent, node) pairs. The first yielded value is (null, this).
        public IEnumerable<KeyValuePair<TreeDumperNode, TreeDumperNode>> PreorderTraversal()
        {
            var stack = new Stack<KeyValuePair<TreeDumperNode, TreeDumperNode>>();
            stack.Push(new KeyValuePair<TreeDumperNode, TreeDumperNode>(null, this));
            while (stack.Count != 0)
            {
                var currentEdge = stack.Pop();
                yield return currentEdge;
                var currentNode = currentEdge.Value;
                foreach (var child in currentNode.Children.Where(x => x != null).Reverse())
                {
                    stack.Push(new KeyValuePair<TreeDumperNode, TreeDumperNode>(currentNode, child));
                }
            }
        }
    }
}