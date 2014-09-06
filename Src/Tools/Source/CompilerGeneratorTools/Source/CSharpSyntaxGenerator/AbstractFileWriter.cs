// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSharpSyntaxGenerator
{
    internal abstract class AbstractFileWriter
    {
        private readonly TextWriter writer;
        private readonly Tree tree;
        private readonly IDictionary<string, string> parentMap;
        private readonly ILookup<string, string> childMap;
        private readonly IDictionary<string, Node> nodeMap;

        private const int INDENT_SIZE = 4;
        private int indentLevel = 0;
        private bool needIndent = true;

        protected AbstractFileWriter(TextWriter writer, Tree tree)
        {
            this.writer = writer;
            this.tree = tree;
            this.nodeMap = tree.Types.OfType<Node>().ToDictionary(n => n.Name);
            this.parentMap = tree.Types.ToDictionary(n => n.Name, n => n.Base);
            this.parentMap.Add(tree.Root, null);
            this.childMap = tree.Types.ToLookup(n => n.Base, n => n.Name);
        }

        protected IDictionary<string, string> ParentMap { get { return parentMap; } }
        protected ILookup<string, string> ChildMap { get { return childMap; } }
        protected Tree Tree { get { return tree; } }

        #region Output helpers

        protected void Indent()
        {
            indentLevel++;
        }

        protected void Unindent()
        {
            if (indentLevel <= 0)
            {
                throw new InvalidOperationException("Cannot unindent from base level");
            }
            indentLevel--;
        }

        protected void Write(string msg)
        {
            WriteIndentIfNeeded();
            writer.Write(msg);
        }

        protected void Write(string msg, params object[] args)
        {
            WriteIndentIfNeeded();
            writer.Write(msg, args);
        }

        protected void WriteLine()
        {
            WriteLine("");
        }

        protected void WriteLine(string msg)
        {
            WriteIndentIfNeeded();
            writer.WriteLine(msg);
            needIndent = true; //need an indent after each line break
        }

        protected void WriteLine(string msg, params object[] args)
        {
            WriteIndentIfNeeded();
            writer.WriteLine(msg, args);
            needIndent = true; //need an indent after each line break
        }

        private void WriteIndentIfNeeded()
        {
            if (needIndent)
            {
                writer.Write(new string(' ', indentLevel * INDENT_SIZE));
                needIndent = false;
            }
        }

        protected void OpenBlock()
        {
            WriteLine("{");
            Indent();
        }

        protected void CloseBlock()
        {
            Unindent();
            WriteLine("}");
        }

        #endregion Output helpers

        #region Node helpers

        protected static string OverrideOrNewModifier(Field field)
        {
            return IsOverride(field) ? "override " : IsNew(field) ? "new " : "";
        }

        protected static bool CanBeField(Field field)
        {
            return field.Type != "SyntaxToken" && !IsAnyList(field.Type) && !IsOverride(field) && !IsNew(field);
        }

        protected static string GetFieldType(Field field)
        {
            if (IsAnyList(field.Type))
                return "CSharpSyntaxNode";
            return field.Type;
        }

        protected bool IsDerivedOrListOfDerived(string baseType, string derivedType)
        {
            return IsDerivedType(baseType, derivedType)
                || ((IsNodeList(derivedType) || IsSeparatedNodeList(derivedType))
                    && IsDerivedType(baseType, GetElementType(derivedType)));
        }

        protected static bool IsSeparatedNodeList(string typeName)
        {
            return typeName.StartsWith("SeparatedSyntaxList<");
        }

        protected static bool IsNodeList(string typeName)
        {
            return typeName.StartsWith("SyntaxList<");
        }

        protected static bool IsAnyNodeList(string typeName)
        {
            return IsNodeList(typeName) || IsSeparatedNodeList(typeName);
        }

        protected bool IsNodeOrNodeList(string typeName)
        {
            return IsNode(typeName) || IsNodeList(typeName) || IsSeparatedNodeList(typeName) || typeName == "SyntaxNodeOrTokenList";
        }

        protected static string GetElementType(string typeName)
        {
            if (!typeName.Contains("<"))
                return string.Empty;
            int iStart = typeName.IndexOf('<');
            int iEnd = typeName.IndexOf('>', iStart + 1);
            if (iEnd < iStart)
                return string.Empty;
            var sub = typeName.Substring(iStart + 1, iEnd - iStart - 1);
            return sub;
        }

        protected static bool IsAnyList(string typeName)
        {
            return IsNodeList(typeName) || IsSeparatedNodeList(typeName) || typeName == "SyntaxNodeOrTokenList";
        }

        protected bool IsDerivedType(string typeName, string derivedTypeName)
        {
            if (typeName == derivedTypeName)
                return true;
            string baseType;
            if (derivedTypeName != null && this.parentMap.TryGetValue(derivedTypeName, out baseType))
            {
                return IsDerivedType(typeName, baseType);
            }
            return false;
        }

        protected static bool IsRoot(Node n)
        {
            return n.Root != null && string.Compare(n.Root, "true", true) == 0;
        }

        protected bool IsNode(string typeName)
        {
            return this.parentMap.ContainsKey(typeName);
        }

        protected Node GetNode(string typeName)
        {
            Node node;
            return this.nodeMap.TryGetValue(typeName, out node) ? node : null;
        }

        protected static bool IsOptional(Field f)
        {
            return f.Optional != null && string.Compare(f.Optional, "true", true) == 0;
        }

        protected static bool IsOverride(Field f)
        {
            return f.Override != null && string.Compare(f.Override, "true", true) == 0;
        }

        protected static bool IsNew(Field f)
        {
            return f.New != null && string.Compare(f.New, "true", true) == 0;
        }

        protected static bool HasErrors(Node n)
        {
            return n.Errors == null || string.Compare(n.Errors, "true", true) == 0;
        }

        protected static string CamelCase(string name)
        {
            if (char.IsUpper(name[0]))
            {
                name = char.ToLowerInvariant(name[0]) + name.Substring(1);
            }
            return FixKeyword(name);
        }

        protected static string FixKeyword(string name)
        {
            if (IsKeyword(name))
            {
                return "@" + name;
            }
            return name;
        }

        protected string StripNode(string name)
        {
            return (tree.Root.EndsWith("Node")) ? tree.Root.Substring(0, tree.Root.Length - 4) : tree.Root;
        }

        protected string StripRoot(string name)
        {
            var root = StripNode(tree.Root);
            if (name.EndsWith(root))
            {
                return name.Substring(0, name.Length - root.Length);
            }
            return name;
        }

        protected static string StripPost(string name, string post)
        {
            return name.EndsWith(post)
                ? name.Substring(0, name.Length - post.Length)
                : name;
        }

        protected static bool IsKeyword(string name)
        {
            switch (name)
            {
                case "bool":
                case "byte":
                case "sbyte":
                case "short":
                case "ushort":
                case "int":
                case "uint":
                case "long":
                case "ulong":
                case "double":
                case "float":
                case "decimal":
                case "string":
                case "char":
                case "object":
                case "typeof":
                case "sizeof":
                case "null":
                case "true":
                case "false":
                case "if":
                case "else":
                case "while":
                case "for":
                case "foreach":
                case "do":
                case "switch":
                case "case":
                case "default":
                case "lock":
                case "try":
                case "throw":
                case "catch":
                case "finally":
                case "goto":
                case "break":
                case "continue":
                case "return":
                case "public":
                case "private":
                case "internal":
                case "protected":
                case "static":
                case "readonly":
                case "sealed":
                case "const":
                case "new":
                case "override":
                case "abstract":
                case "virtual":
                case "partial":
                case "ref":
                case "out":
                case "in":
                case "where":
                case "params":
                case "this":
                case "base":
                case "namespace":
                case "using":
                case "class":
                case "struct":
                case "interface":
                case "delegate":
                case "checked":
                case "get":
                case "set":
                case "add":
                case "remove":
                case "operator":
                case "implicit":
                case "explicit":
                case "fixed":
                case "extern":
                case "event":
                case "enum":
                case "unsafe":
                    return true;
                default:
                    return false;
            }
        }

        #endregion Node helpers
    }
}