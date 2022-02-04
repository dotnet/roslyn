// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests
{
    internal static class CSharpServiceTestExtensions
    {
        /// <summary>
        /// DFS search to find the first node of a given type.
        /// </summary>
        internal static T FindFirstNodeOfType<T>(this SyntaxNode node)
            where T : SyntaxNode
        {
            if (node is T)
            {
                return node as T;
            }

            foreach (var child in node.ChildNodes())
            {
                var foundNode = child.FindFirstNodeOfType<T>();
                if (foundNode != null)
                {
                    return foundNode;
                }
            }

            return null;
        }

        internal static T DigToFirstNodeOfType<T>(this SyntaxNode node)
            where T : SyntaxNode
        {
            return node.ChildNodes().OfType<T>().First();
        }

        internal static T DigToFirstNodeOfType<T>(this SyntaxTree syntaxTree)
            where T : SyntaxNode
        {
            return syntaxTree.GetRoot().DigToFirstNodeOfType<T>();
        }

        internal static TypeDeclarationSyntax DigToFirstTypeDeclaration(this SyntaxTree syntaxTree)
            => (syntaxTree.GetRoot() as CompilationUnitSyntax).Members.OfType<TypeDeclarationSyntax>().First();
    }
}
