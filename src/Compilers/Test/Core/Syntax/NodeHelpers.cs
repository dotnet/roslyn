// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static partial class NodeHelpers
    {
        // TODO: Having this as a shared property makes this less discoverable. This can also be bad
        // if we ever have an app where multiple threads need to use this with each thread operating
        // on trees from a different language. We will need to fix this if we ever need to write app
        // test that does something like this.
        public static ISyntaxNodeKindProvider KindProvider
        {
            get;
            set;
        }

        public static string GetKind(this SyntaxNodeOrToken n)
        {
            return KindProvider.Kind(n);
        }

        public static string GetKind(this SyntaxNode n)
        {
            return KindProvider.Kind(n);
        }

        public static string GetKind(this SyntaxToken n)
        {
            return KindProvider.Kind(n);
        }

        public static string GetKind(this SyntaxTrivia n)
        {
            return KindProvider.Kind(n);
        }

        public static bool IsIdentifier(this SyntaxToken n)
        {
            return n.GetKind().Contains("Identifier") && n.Parent != null && n.Parent.GetKind().Contains("Name");
        }

        public static bool IsKeyword(this SyntaxToken n)
        {
            var kind = n.GetKind();
            return kind.EndsWith("Keyword", StringComparison.Ordinal) || (kind.Contains("Identifier") && n.Parent != null && !n.Parent.GetKind().Contains("Name"));
        }

        public static bool IsLiteral(this SyntaxToken n)
        {
            return n.GetKind().Contains("Literal");
        }

        public static bool IsComment(this SyntaxTrivia n)
        {
            return n.GetKind().Contains("Comment");
        }

        public static bool IsDocumentationComment(this SyntaxTrivia n)
        {
            return n.GetKind().Contains("DocumentationComment");
        }

        public static bool IsDisabledOrSkippedText(this SyntaxTrivia n)
        {
            var kind = n.GetKind();
            return kind.Contains("Disabled") || kind.Contains("Skipped");
        }

        public static SyntaxNode GetRootNode(this SyntaxNodeOrToken node)
        {
            SyntaxNode retVal = null;
            if (node.IsNode)
            {
                retVal = node.AsNode().GetRootNode();
            }
            else
            {
                retVal = node.AsToken().GetRootNode();
            }

            return retVal;
        }

        public static SyntaxNode GetRootNode(this SyntaxToken node)
        {
            SyntaxNode retVal = node.Parent;
            if (retVal != null)
            {
                while (retVal.Parent != null)
                {
                    retVal = retVal.Parent;
                }
            }

            return retVal;
        }

        public static SyntaxNode GetRootNode(this SyntaxNode node)
        {
            var retVal = node.Parent == null ? node : node.Parent;
            while (retVal.Parent != null)
            {
                retVal = retVal.Parent;
            }

            return retVal;
        }

        public static SyntaxNode GetRootNode(this SyntaxTrivia node)
        {
            SyntaxNode retVal = node.Token.Parent;
            if (retVal != null)
            {
                while (retVal != null)
                {
                    retVal = retVal.Parent;
                }
            }

            return retVal;
        }

        public static NodeInfo GetNodeInfo(this SyntaxNodeOrToken nodeOrToken)
        {
            NodeInfo retVal = null;
            if (nodeOrToken.IsNode)
            {
                retVal = GetNodeInfo(nodeOrToken.AsNode());
            }
            else
            {
                retVal = GetNodeInfo(nodeOrToken.AsToken());
            }

            return retVal;
        }

        public static NodeInfo GetNodeInfo(this SyntaxNode node)
        {
            var typeObject = node.GetType();
            var nodeClassName = typeObject.Name;
            var properties = typeObject.GetTypeInfo().DeclaredProperties;
            return new NodeInfo(typeObject.Name, (
                from p in properties
                where IsField(p)
                select GetFieldInfo(p, node)).ToArray());
        }

        public static NodeInfo GetNodeInfo(this SyntaxToken token)
        {
            var typeObject = token.GetType();
            var nodeClassName = typeObject.Name;
            var properties = typeObject.GetTypeInfo().DeclaredProperties;
            return new NodeInfo(typeObject.Name, (
                from p in properties
                where IsField(p)
                select GetFieldInfo(p, token)).ToArray());
        }

        public static NodeInfo GetNodeInfo(this SyntaxTrivia trivia)
        {
            var typeObject = trivia.GetType();
            var nodeClassName = typeObject.Name;
            var properties = typeObject.GetTypeInfo().DeclaredProperties;
            return new NodeInfo(typeObject.Name, (
                from p in properties
                where IsField(p)
                select GetFieldInfo(p, trivia)).ToArray());
        }

        //Does this property refer to a field?
        private static bool IsField(PropertyInfo prop)
        {
            var typeObject = prop.PropertyType;
            if (typeObject == typeof(int) ||
                typeObject == typeof(uint) ||
                typeObject == typeof(long) ||
                typeObject == typeof(ulong) ||
                typeObject == typeof(bool) ||
                typeObject == typeof(string) ||
                typeObject == typeof(float) ||
                typeObject == typeof(double) ||
                typeObject == typeof(char) ||
                typeObject == typeof(DateTime) ||
                typeObject == typeof(decimal) ||
                typeObject.GetTypeInfo().IsEnum)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //Only called if IsField returns true. Get the name/type/value of this field and packages into a FieldInfo.
        private static NodeInfo.FieldInfo GetFieldInfo(PropertyInfo prop, SyntaxNode node)
        {
            return new NodeInfo.FieldInfo(prop.Name, prop.PropertyType, prop.GetValue(node, null));
        }

        //Only called if IsField returns true. Get the name/type/value of this field and packages into a FieldInfo.
        private static NodeInfo.FieldInfo GetFieldInfo(PropertyInfo prop, SyntaxToken token)
        {
            return new NodeInfo.FieldInfo(prop.Name, prop.PropertyType, prop.GetValue(token, null));
        }

        //Only called if IsField returns true. Get the name/type/value of this field and packages into a FieldInfo.
        private static NodeInfo.FieldInfo GetFieldInfo(PropertyInfo prop, SyntaxTrivia trivia)
        {
            return new NodeInfo.FieldInfo(prop.Name, prop.PropertyType, prop.GetValue(trivia, null));
        }
    }
}
