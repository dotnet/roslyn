// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpSyntaxGenerator
{
    internal sealed class SignatureWriter
    {
        private readonly Tree _tree;
        private readonly Dictionary<string, string> _typeMap;

        private SignatureWriter(Tree tree)
        {
            _tree = tree;
            _typeMap = tree.Types.ToDictionary(n => n.Name, n => n.Base);
            _typeMap.Add(tree.Root, null);
        }

        public static void Write(TextWriter writer, Tree tree)
        {
            new SignatureWriter(tree).WriteFile(writer);
        }

        private void WriteFile(TextWriter writer)
        {
            using var builder = new IndentingStringBuilder();

            builder.WriteLine("using System;");
            builder.WriteLine("using System.Collections;");
            builder.WriteLine("using System.Collections.Generic;");
            builder.WriteLine("using System.Linq;");
            builder.WriteLine("using System.Threading;");
            builder.WriteLine();
            builder.WriteLine("namespace Microsoft.CodeAnalysis.CSharp");
            using (builder.EnterBlock())
            {
                builder.WriteBlankLineSeparated(
                    _tree.Types.Where(n => n is not PredefinedNode),
                    static (builder, node, @this) => @this.WriteType(builder, node),
                    this);
            }

            writer.Write(builder.ToString());
        }

        private void WriteType(IndentingStringBuilder builder, TreeType treeType)
        {
            if (treeType is AbstractNode abstractNode)
            {
                builder.WriteLine($"public abstract partial class {treeType.Name} : {treeType.Base}");
                using var _ = builder.EnterBlock();

                foreach (var field in abstractNode.Fields)
                    builder.WriteLine($$"""public abstract {{field.Type}} {{field.Name}} { get; }""");
            }
            else if (treeType is Node node)
            {
                builder.WriteLine($"public partial class {treeType.Name} : {treeType.Base}");
                using var _ = builder.EnterBlock();

                if (node.Kinds.Count > 1)
                {
                    foreach (var kind in node.Kinds)
                        builder.WriteLine($"// {kind.Name}");
                }

                foreach (var field in node.Fields.Where(n => IsNodeOrNodeList(n.Type)))
                    builder.WriteLine($$"""public {{field.Type}} {{field.Name}} { get; }""");

                foreach (var field in node.Fields.Where(n => !IsNodeOrNodeList(n.Type)))
                    builder.WriteLine($$"""public {{field.Type}} {{field.Name}} { get; }""");
            }
        }

        private static bool IsSeparatedNodeList(string typeName)
            => typeName.StartsWith("SeparatedSyntaxList<", StringComparison.Ordinal);

        private static bool IsNodeList(string typeName)
            => typeName.StartsWith("SyntaxList<", StringComparison.Ordinal);

        public bool IsNodeOrNodeList(string typeName)
            => IsNode(typeName) || IsNodeList(typeName) || IsSeparatedNodeList(typeName);

        private bool IsNode(string typeName)
            => _typeMap.ContainsKey(typeName);
    }
}
