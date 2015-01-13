// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSharpSyntaxGenerator
{
    class SignatureWriter
    {
        private readonly TextWriter writer;
        private readonly Tree tree;
        private readonly Dictionary<string, string> typeMap;

        private SignatureWriter(TextWriter writer, Tree tree)
        {
            this.writer = writer;
            this.tree = tree;
            this.typeMap = tree.Types.ToDictionary(n => n.Name, n => n.Base);
            this.typeMap.Add(tree.Root, null);
        }

        public static void Write(TextWriter writer, Tree tree)
        {
            new SignatureWriter(writer, tree).WriteFile();
        }

        private void WriteFile()
        {
            writer.WriteLine("using System;");
            writer.WriteLine("using System.Collections;");
            writer.WriteLine("using System.Collections.Generic;");
            writer.WriteLine("using System.Linq;");
            writer.WriteLine("using System.Threading;");
            writer.WriteLine();
            writer.WriteLine("namespace Microsoft.CodeAnalysis.CSharp");
            writer.WriteLine("{");

            this.WriteTypes();

            writer.WriteLine("}");
        }

        private void WriteTypes()
        {
            var nodes = tree.Types.Where(n => !(n is PredefinedNode)).ToList();
            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                var node = nodes[i];
                writer.WriteLine();
                this.WriteType(node);
            }
        }

        private void WriteType(TreeType node)
        {
            if (node is AbstractNode)
            {
                AbstractNode nd = (AbstractNode)node;
                writer.WriteLine("  public abstract partial class {0} : {1}", node.Name, node.Base);
                writer.WriteLine("  {");
                for (int i = 0, n = nd.Fields.Count; i < n; i++)
                {
                    var field = nd.Fields[i];
                    if (IsNodeOrNodeList(field.Type))
                    {
                        writer.WriteLine("    public abstract {0}{1} {2} {{ get; }}", "", field.Type, field.Name);
                    }
                }
                writer.WriteLine("  }");
            }
            else if (node is Node)
            {
                Node nd = (Node)node;
                writer.WriteLine("  public partial class {0} : {1}", node.Name, node.Base);
                writer.WriteLine("  {");

                WriteKinds(nd.Kinds);

                var valueFields = nd.Fields.Where(n => !IsNodeOrNodeList(n.Type)).ToList();
                var nodeFields = nd.Fields.Where(n => IsNodeOrNodeList(n.Type)).ToList();

                for (int i = 0, n = nodeFields.Count; i < n; i++)
                {
                    var field = nodeFields[i];
                    writer.WriteLine("    public {0}{1}{2} {3} {{ get; }}", "", "", field.Type, field.Name);
                }

                for (int i = 0, n = valueFields.Count; i < n; i++)
                {
                    var field = valueFields[i];
                    writer.WriteLine("    public {0}{1}{2} {3} {{ get; }}", "", "", field.Type, field.Name);
                }

                writer.WriteLine("  }");
            }
        }

        private void WriteKinds(List<Kind> kinds)
        {
            if (kinds.Count > 1)
            {
                foreach (var kind in kinds)
                {
                    writer.WriteLine("    // {0}", kind.Name);
                }
            }
        }

        private bool IsSeparatedNodeList(string typeName)
        {
            return typeName.StartsWith("SeparatedSyntaxList<");
        }

        private bool IsNodeList(string typeName)
        {
            return typeName.StartsWith("SyntaxList<");
        }

        public bool IsNodeOrNodeList(string typeName)
        {
            return IsNode(typeName) || IsNodeList(typeName) || IsSeparatedNodeList(typeName);
        }

        private bool IsNode(string typeName)
        {
            return this.typeMap.ContainsKey(typeName);
        }
    }
}
