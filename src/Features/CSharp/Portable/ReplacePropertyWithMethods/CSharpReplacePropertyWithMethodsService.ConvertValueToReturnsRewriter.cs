// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ReplacePropertyWithMethods
{
    internal partial class CSharpReplacePropertyWithMethodsService
    {
        private class ConvertValueToReturnsRewriter : CSharpSyntaxRewriter
        {
            public static readonly CSharpSyntaxRewriter Instance = new ConvertValueToReturnsRewriter();

            private ConvertValueToReturnsRewriter()
            {
            }

            private static XmlNameSyntax ConvertToReturns(XmlNameSyntax name)
                => name.ReplaceToken(name.LocalName, SyntaxFactory.Identifier("returns"));

            public override SyntaxNode VisitXmlElementStartTag(XmlElementStartTagSyntax node)
                => IsValueName(node.Name)
                    ? node.ReplaceNode(node.Name, ConvertToReturns(node.Name))
                    : base.VisitXmlElementStartTag(node);

            public override SyntaxNode VisitXmlElementEndTag(XmlElementEndTagSyntax node)
                => IsValueName(node.Name)
                    ? node.ReplaceNode(node.Name, ConvertToReturns(node.Name))
                    : base.VisitXmlElementEndTag(node);
        }
    }
}
