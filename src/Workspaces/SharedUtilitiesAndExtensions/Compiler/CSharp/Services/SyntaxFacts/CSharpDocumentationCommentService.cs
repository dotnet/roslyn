// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp.LanguageService;

internal sealed class CSharpDocumentationCommentService() : AbstractDocumentationCommentService<
    DocumentationCommentTriviaSyntax,
    XmlNodeSyntax,
    XmlAttributeSyntax,
    CrefSyntax,
    XmlElementSyntax,
    XmlTextSyntax,
    XmlEmptyElementSyntax,
    XmlCrefAttributeSyntax,
    XmlNameAttributeSyntax,
    XmlTextAttributeSyntax>(CSharpSyntaxFacts.Instance)
{
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
