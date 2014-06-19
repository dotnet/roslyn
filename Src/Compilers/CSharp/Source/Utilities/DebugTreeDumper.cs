//-----------------------------------------------------------------------------------------------------------
//
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//
//-----------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
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

    internal sealed class TreeDumperNode
    {
        public TreeDumperNode(string text, object value, IEnumerable<TreeDumperNode> children)
        {
            this.Text = text;
            this.Value = value;
            this.Children = children ?? Enumerable.Empty<TreeDumperNode>();
        }

        public TreeDumperNode(string text) : this(text, null, null) { }
        public object Value { get; private set; }
        public string Text { get; private set; }
        public IEnumerable<TreeDumperNode> Children { get; private set; }
    }
}
