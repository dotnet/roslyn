// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            private XmlNameSyntax ConvertToReturns(XmlNameSyntax name)
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
