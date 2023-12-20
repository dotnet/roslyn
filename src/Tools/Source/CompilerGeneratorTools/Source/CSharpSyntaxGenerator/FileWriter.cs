// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CSharpSyntaxGenerator
{
    internal sealed class FileWriter
    {
        private readonly Tree _tree;
        private readonly Dictionary<string, string> _parentMap;
        private readonly ILookup<string, string> _childMap;

        private readonly Dictionary<string, Node> _nodeMap;
        private readonly Dictionary<string, TreeType> _typeMap;

        public FileWriter(Tree tree, CancellationToken cancellationToken)
        {
            _tree = tree;
            _nodeMap = tree.Types.OfType<Node>().ToDictionary(n => n.Name);
            _typeMap = tree.Types.ToDictionary(n => n.Name);
            _parentMap = tree.Types.ToDictionary(n => n.Name, n => n.Base);
            _parentMap.Add(tree.Root, null);
            _childMap = tree.Types.ToLookup(n => n.Base, n => n.Name);

            CancellationToken = cancellationToken;
        }

        public IDictionary<string, string> ParentMap { get { return _parentMap; } }
        public ILookup<string, string> ChildMap { get { return _childMap; } }
        public Tree Tree { get { return _tree; } }
        public CancellationToken CancellationToken { get; }

        #region Node helpers

        public static string OverrideOrNewModifier(Field field)
        {
            return IsOverride(field) ? "override " : IsNew(field) ? "new " : "";
        }

        public static bool CanBeField(Field field)
        {
            return field.Type != "SyntaxToken" && !IsAnyList(field.Type) && !IsOverride(field) && !IsNew(field);
        }

        public static string GetFieldType(Field field, bool green)
        {
            // Fields in red trees are lazily initialized, with null as the uninitialized value
            return getNullableAwareType(field.Type, optionalOrLazy: IsOptional(field) || !green, green);

            static string getNullableAwareType(string fieldType, bool optionalOrLazy, bool green)
            {
                if (IsAnyList(fieldType))
                {
                    if (optionalOrLazy)
                        return green ? "GreenNode?" : "SyntaxNode?";
                    else
                        return green ? "GreenNode?" : "SyntaxNode";
                }

                switch (fieldType)
                {
                    case var _ when !optionalOrLazy:
                        return fieldType;

                    case "bool":
                    case "SyntaxToken" when !green:
                        return fieldType;

                    default:
                        return fieldType + "?";
                }
            }
        }

        public bool IsDerivedOrListOfDerived(string baseType, string derivedType)
        {
            return IsDerivedType(baseType, derivedType)
                || ((IsNodeList(derivedType) || IsSeparatedNodeList(derivedType))
                    && IsDerivedType(baseType, GetElementType(derivedType)));
        }

        public static bool IsSeparatedNodeList(string typeName)
        {
            return typeName.StartsWith("SeparatedSyntaxList<", StringComparison.Ordinal);
        }

        public static bool IsNodeList(string typeName)
        {
            return typeName.StartsWith("SyntaxList<", StringComparison.Ordinal);
        }

        public static bool IsAnyNodeList(string typeName)
        {
            return IsNodeList(typeName) || IsSeparatedNodeList(typeName);
        }

        public bool IsNodeOrNodeList(string typeName)
        {
            return IsNode(typeName) || IsNodeList(typeName) || IsSeparatedNodeList(typeName) || typeName == "SyntaxNodeOrTokenList";
        }

        public static string GetElementType(string typeName)
        {
            if (!typeName.Contains('<'))
                return string.Empty;

            int iStart = typeName.IndexOf('<');
            int iEnd = typeName.IndexOf('>', iStart + 1);
            if (iEnd < iStart)
                return string.Empty;

            return typeName[(iStart + 1)..iEnd];
        }

        public static bool IsAnyList(string typeName)
        {
            return IsNodeList(typeName) || IsSeparatedNodeList(typeName) || typeName == "SyntaxNodeOrTokenList";
        }

        public bool IsDerivedType(string typeName, string derivedTypeName)
        {
            if (typeName == derivedTypeName)
                return true;

            if (derivedTypeName != null && _parentMap.TryGetValue(derivedTypeName, out var baseType))
                return IsDerivedType(typeName, baseType);

            return false;
        }

        public static bool IsRoot(Node n)
        {
            return n.Root != null && string.Compare(n.Root, "true", true) == 0;
        }

        public bool IsNode(string typeName)
        {
            return _parentMap.ContainsKey(typeName);
        }

        public Node GetNode(string typeName)
            => _nodeMap.TryGetValue(typeName, out var node) ? node : null;

        public TreeType GetTreeType(string typeName)
            => _typeMap.TryGetValue(typeName, out var node) ? node : null;

        private static bool IsTrue(string val)
            => val != null && string.Compare(val, "true", true) == 0;

        public static bool IsOptional(Field f)
            => IsTrue(f.Optional);

        public static bool IsOverride(Field f)
            => IsTrue(f.Override);

        public static bool IsNew(Field f)
            => IsTrue(f.New);

        public static bool HasErrors(Node n)
        {
            return n.Errors == null || string.Compare(n.Errors, "true", true) == 0;
        }

        public static string CamelCase(string name)
        {
            if (char.IsUpper(name[0]))
                name = char.ToLowerInvariant(name[0]) + name[1..];

            return FixKeyword(name);
        }

        public static string FixKeyword(string name)
            => IsKeyword(name) ? "@" + name : name;

        public static string StripPost(string name, string post)
        {
            return name.EndsWith(post, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - post.Length)
                : name;
        }

        public static bool IsKeyword(string name)
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

        public List<Kind> GetKindsOfFieldOrNearestParent(TreeType nd, Field field)
        {
            while ((field.Kinds is null || field.Kinds.Count == 0) && IsOverride(field))
            {
                nd = GetTreeType(nd.Base);
                field = (nd switch
                {
                    Node node => node.Fields,
                    AbstractNode abstractNode => abstractNode.Fields,
                    _ => throw new InvalidOperationException("Unexpected node type.")
                }).Single(f => f.Name == field.Name);
            }

            return field.Kinds.Distinct().ToList();
        }

        #endregion Node helpers
    }
}
