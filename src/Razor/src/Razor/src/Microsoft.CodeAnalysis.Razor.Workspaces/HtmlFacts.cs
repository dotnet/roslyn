// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Editor.Razor;

internal static class HtmlFacts
{
    private static readonly FrozenSet<string> s_htmlSchemaTagNames = new string[]
    {
        "DOCTYPE",
        "a",
        "abbr",
        "acronym",
        "address",
        "applet",
        "area",
        "article",
        "aside",
        "audio",
        "b",
        "base",
        "basefont",
        "bdi",
        "bdo",
        "big",
        "blockquote",
        "body",
        "br",
        "button",
        "canvas",
        "caption",
        "center",
        "cite",
        "code",
        "col",
        "colgroup",
        "data",
        "datalist",
        "dd",
        "del",
        "details",
        "dfn",
        "dialog",
        "dir",
        "div",
        "dl",
        "dt",
        "em",
        "embed",
        "fieldset",
        "figcaption",
        "figure",
        "font",
        "footer",
        "form",
        "frame",
        "frameset",
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "head",
        "header",
        "hr",
        "html",
        "i",
        "iframe",
        "img",
        "input",
        "ins",
        "kbd",
        "label",
        "legend",
        "li",
        "link",
        "main",
        "map",
        "mark",
        "meta",
        "meter",
        "nav",
        "noframes",
        "noscript",
        "object",
        "ol",
        "optgroup",
        "option",
        "output",
        "p",
        "param",
        "picture",
        "pre",
        "progress",
        "q",
        "rp",
        "rt",
        "ruby",
        "s",
        "samp",
        "script",
        "section",
        "select",
        "small",
        "source",
        "span",
        "strike",
        "strong",
        "style",
        "sub",
        "summary",
        "sup",
        "svg",
        "table",
        "tbody",
        "td",
        "template",
        "textarea",
        "tfoot",
        "th",
        "thead",
        "time",
        "title",
        "tr",
        "track",
        "tt",
        "u",
        "ul",
        "var",
        "video",
        "wbr",
    }.ToFrozenSet(StringComparer.Ordinal);

    internal static readonly ImmutableArray<string> FormEvents =
    [
        "onabort",
        "onblur",
        "onchange",
        "onclick",
        "oncontextmenu",
        "ondblclick",
        "onerror",
        "onfocus",
        "oninput",
        "onkeydown",
        "onkeypress",
        "onkeyup",
        "onload",
        "onmousedown",
        "onmousemove",
        "onmouseout",
        "onmouseover",
        "onmouseup",
        "onreset",
        "onscroll",
        "onselect",
        "onsubmit",
    ];

    public static bool IsHtmlTagName(string name)
        => s_htmlSchemaTagNames.Contains(name);

    public static bool TryGetElementInfo(
        SyntaxNode element,
        out SyntaxToken containingTagNameToken,
        out SyntaxList<RazorSyntaxNode> attributeNodes,
        out SyntaxToken closingForwardSlashOrCloseAngleToken)
    {
        switch (element)
        {
            case BaseMarkupStartTagSyntax startTag:
                {
                    containingTagNameToken = startTag.Name;
                    attributeNodes = startTag.Attributes;

                    closingForwardSlashOrCloseAngleToken = startTag.ForwardSlash.IsValid(out var forwardSlash)
                        ? forwardSlash
                        : startTag.CloseAngle;
                }

                return true;

            case BaseMarkupEndTagSyntax endTag:
                {
                    containingTagNameToken = endTag.Name;
                    attributeNodes = endTag.GetStartTag()?.Attributes ?? default;

                    closingForwardSlashOrCloseAngleToken = endTag.ForwardSlash.IsValid(out var forwardSlash)
                        ? forwardSlash
                        : endTag.CloseAngle;
                }

                return true;

            default:
                containingTagNameToken = default;
                attributeNodes = default;
                closingForwardSlashOrCloseAngleToken = default;
                return false;
        }
    }

    public static bool TryGetAttributeInfo(
        SyntaxNode attribute,
        out SyntaxToken containingTagNameToken,
        out TextSpan? prefixLocation,
        out string? selectedAttributeName,
        out TextSpan? selectedAttributeNameLocation,
        out SyntaxList<RazorSyntaxNode> attributeNodes)
    {
        if (!TryGetElementInfo(attribute.Parent, out containingTagNameToken, out attributeNodes, closingForwardSlashOrCloseAngleToken: out _))
        {
            containingTagNameToken = default;
            prefixLocation = null;
            selectedAttributeName = null;
            selectedAttributeNameLocation = null;
            attributeNodes = default;
            return false;
        }

        switch (attribute)
        {
            case MarkupMinimizedAttributeBlockSyntax minimizedAttributeBlock:
                prefixLocation = minimizedAttributeBlock.NamePrefix?.Span;
                selectedAttributeName = minimizedAttributeBlock.Name.GetContent();
                selectedAttributeNameLocation = minimizedAttributeBlock.Name.Span;
                return true;
            case MarkupAttributeBlockSyntax attributeBlock:
                prefixLocation = attributeBlock.NamePrefix?.Span;
                selectedAttributeName = attributeBlock.Name.GetContent();
                selectedAttributeNameLocation = attributeBlock.Name.Span;
                return true;
            case MarkupTagHelperAttributeSyntax tagHelperAttribute:
                prefixLocation = tagHelperAttribute.NamePrefix?.Span;
                selectedAttributeName = tagHelperAttribute.Name.GetContent();
                selectedAttributeNameLocation = tagHelperAttribute.Name.Span;
                return true;
            case MarkupMinimizedTagHelperAttributeSyntax minimizedAttribute:
                prefixLocation = minimizedAttribute.NamePrefix?.Span;
                selectedAttributeName = minimizedAttribute.Name.GetContent();
                selectedAttributeNameLocation = minimizedAttribute.Name.Span;
                return true;
            case MarkupTagHelperDirectiveAttributeSyntax tagHelperDirectiveAttribute:
                {
                    prefixLocation = tagHelperDirectiveAttribute.NamePrefix?.Span;
                    selectedAttributeName = tagHelperDirectiveAttribute.FullName;
                    var fullNameSpan = TextSpan.FromBounds(tagHelperDirectiveAttribute.Transition.Span.Start, tagHelperDirectiveAttribute.Name.Span.End);
                    selectedAttributeNameLocation = fullNameSpan;
                    return true;
                }
            case MarkupMinimizedTagHelperDirectiveAttributeSyntax minimizedTagHelperDirectiveAttribute:
                {
                    prefixLocation = minimizedTagHelperDirectiveAttribute.NamePrefix?.Span;
                    selectedAttributeName = minimizedTagHelperDirectiveAttribute.FullName;
                    var fullNameSpan = TextSpan.FromBounds(minimizedTagHelperDirectiveAttribute.Transition.Span.Start, minimizedTagHelperDirectiveAttribute.Name.Span.End);
                    selectedAttributeNameLocation = fullNameSpan;
                    return true;
                }
            case MarkupMiscAttributeContentSyntax:
                prefixLocation = null;
                selectedAttributeName = null;
                selectedAttributeNameLocation = null;
                return true;
        }

        // Not an attribute type that we know of
        prefixLocation = null;
        selectedAttributeName = null;
        selectedAttributeNameLocation = null;
        return false;
    }
}
