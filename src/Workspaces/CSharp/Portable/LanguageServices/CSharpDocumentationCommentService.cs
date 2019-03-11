// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class CSharpDocumentationCommentService : AbstractDocumentationCommentService<
        DocumentationCommentTriviaSyntax,
        XmlNodeSyntax,
        XmlAttributeSyntax,
        CrefSyntax,
        XmlElementSyntax,
        XmlTextSyntax,
        XmlEmptyElementSyntax,
        XmlCrefAttributeSyntax,
        XmlNameAttributeSyntax,
        XmlTextAttributeSyntax>
    {
        private CSharpDocumentationCommentService()
            : base(CSharpSyntaxFactsService.Instance)
        {
        }

        public static readonly IDocumentationCommentService Instance = new CSharpDocumentationCommentService();

        protected override SyntaxList<XmlAttributeSyntax> GetAttributes(XmlEmptyElementSyntax xmlEmpty)
            => xmlEmpty.Attributes;

        protected override CrefSyntax GetCref(XmlCrefAttributeSyntax xmlCref)
            => xmlCref.Cref;

        protected override SyntaxToken GetIdentifier(XmlNameAttributeSyntax xmlName)
            => xmlName.Identifier.Identifier;

        protected override SyntaxNode GetName(XmlElementSyntax xmlElement)
            => xmlElement.StartTag.Name;

        protected override SyntaxTokenList GetTextTokens(XmlTextAttributeSyntax xmlTextAttribute)
            => xmlTextAttribute.TextTokens;

        protected override SyntaxTokenList GetTextTokens(XmlTextSyntax xmlText)
            => xmlText.TextTokens;
    }
}
