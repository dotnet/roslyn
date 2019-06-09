// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ReplacePropertyWithMethods
{
    internal partial class CSharpReplacePropertyWithMethodsService
    {
        private class ConvertValueToParamRewriter : CSharpSyntaxRewriter
        {
            public static readonly CSharpSyntaxRewriter Instance = new ConvertValueToParamRewriter();

            private ConvertValueToParamRewriter()
            {
            }

            private XmlNameSyntax ConvertToParam(XmlNameSyntax name)
                => name.ReplaceToken(name.LocalName, SyntaxFactory.Identifier("param"));

            public override SyntaxNode VisitXmlElementStartTag(XmlElementStartTagSyntax node)
            {
                if (!IsValueName(node.Name))
                    return base.VisitXmlElementStartTag(node);

                return node.ReplaceNode(node.Name, ConvertToParam(node.Name))
                    .AddAttributes(SyntaxFactory.XmlNameAttribute("value"));
            }

            public override SyntaxNode VisitXmlElementEndTag(XmlElementEndTagSyntax node)
                => IsValueName(node.Name)
                    ? node.ReplaceNode(node.Name, ConvertToParam(node.Name))
                    : base.VisitXmlElementEndTag(node);
        }
    }
}
