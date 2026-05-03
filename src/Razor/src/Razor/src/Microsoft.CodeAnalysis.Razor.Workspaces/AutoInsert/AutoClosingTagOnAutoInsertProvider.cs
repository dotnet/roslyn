// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;

using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.AutoInsert;

internal class AutoClosingTagOnAutoInsertProvider : IOnAutoInsertProvider
{
    // From http://dev.w3.org/html5/spec/Overview.html#elements-0
    private static readonly ImmutableHashSet<string> s_voidElements = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,
        "area",
        "base",
        "br",
        "col",
        "command",
        "embed",
        "hr",
        "img",
        "input",
        "keygen",
        "link",
        "meta",
        "menuitem",
        "param",
        "source",
        "track",
        "wbr"
    );

    private static readonly ImmutableHashSet<string> s_voidElementsCaseSensitive = s_voidElements.WithComparer(StringComparer.Ordinal);

    public string TriggerCharacter => ">";

    public bool TryResolveInsertion(
        Position position,
        RazorCodeDocument codeDocument,
        bool enableAutoClosingTags,
        [NotNullWhen(true)] out VSInternalDocumentOnAutoInsertResponseItem? autoInsertEdit)
    {
        autoInsertEdit = null;

        var sourceText = codeDocument.Source.Text;

        if (!enableAutoClosingTags ||
            !sourceText.TryGetAbsoluteIndex(position, out var afterCloseAngleIndex) ||
            TryResolveAutoClosingBehavior(codeDocument, afterCloseAngleIndex) is not { } tagNameWithClosingBehavior)
        {
            return false;
        }

        if (tagNameWithClosingBehavior.AutoClosingBehavior == AutoClosingBehavior.EndTag)
        {
            var formatForEndTag = InsertTextFormat.Snippet;
            var editForEndTag = LspFactory.CreateTextEdit(position, $"$0</{tagNameWithClosingBehavior.TagName}>");

            autoInsertEdit = new()
            {
                TextEdit = editForEndTag,
                TextEditFormat = formatForEndTag
            };

            return true;
        }

        Debug.Assert(tagNameWithClosingBehavior.AutoClosingBehavior == AutoClosingBehavior.SelfClosing);

        var format = InsertTextFormat.Plaintext;

        // Need to replace the `>` with ' />$0' or '/>$0' depending on if there's prefixed whitespace.
        var insertionText = char.IsWhiteSpace(sourceText[afterCloseAngleIndex - 2]) ? "/" : " /";
        var edit = LspFactory.CreateTextEdit(position.Line, position.Character - 1, insertionText);

        autoInsertEdit = new()
        {
            TextEdit = edit,
            TextEditFormat = format
        };

        return true;
    }

    private static TagNameWithClosingBehavior? TryResolveAutoClosingBehavior(RazorCodeDocument codeDocument, int afterCloseAngleIndex)
    {
        var syntaxRoot = codeDocument.GetRequiredSyntaxRoot();
        var closeAngle = syntaxRoot.FindToken(afterCloseAngleIndex - 1);

        if (closeAngle.Parent is MarkupStartTagSyntax
            {
                ForwardSlash: not { Kind: SyntaxKind.ForwardSlash, IsMissing: false },
                Parent: MarkupElementSyntax htmlElement
            } startTag)
        {
            var unescapedTagName = startTag.Name.Content;
            var autoClosingBehavior = InferAutoClosingBehavior(unescapedTagName, caseSensitive: false);

            if (autoClosingBehavior == AutoClosingBehavior.EndTag && !CouldAutoCloseParentOrSelf(unescapedTagName, htmlElement))
            {
                // Auto-closing behavior is end-tag; however, we already have and end-tag therefore we don't need to do anything!
                return default;
            }

            // Finally capture the entire tag name with the potential escape operator.
            var name = startTag.GetTagNameWithOptionalBang();
            return new TagNameWithClosingBehavior(name, autoClosingBehavior);
        }

        if (closeAngle.Parent is MarkupTagHelperStartTagSyntax
            {
                ForwardSlash: not { Kind: SyntaxKind.ForwardSlash, IsMissing: false },
                Parent: MarkupTagHelperElementSyntax { TagHelperInfo.BindingResult: var binding } tagHelperElement
            } startTagHelper)
        {
            var name = startTagHelper.Name.Content;

            if (!TryGetTagHelperAutoClosingBehavior(binding, out var autoClosingBehavior))
            {
                autoClosingBehavior = InferAutoClosingBehavior(name, caseSensitive: true);
            }

            if (autoClosingBehavior == AutoClosingBehavior.EndTag && !CouldAutoCloseParentOrSelf(name, tagHelperElement))
            {
                // Auto-closing behavior is end-tag; however, we already have and end-tag therefore we don't need to do anything!
                return default;
            }

            return new TagNameWithClosingBehavior(name, autoClosingBehavior);
        }

        return default;
    }

    private static AutoClosingBehavior InferAutoClosingBehavior(string name, bool caseSensitive)
    {
        var voidElements = caseSensitive ? s_voidElementsCaseSensitive : s_voidElements;

        if (voidElements.Contains(name))
        {
            return AutoClosingBehavior.SelfClosing;
        }

        return AutoClosingBehavior.EndTag;
    }

    private static bool TryGetTagHelperAutoClosingBehavior(TagHelperBinding bindingResult, out AutoClosingBehavior autoClosingBehavior)
    {
        var resolvedTagStructure = TagStructure.Unspecified;

        foreach (var boundRulesInfo in bindingResult.AllBoundRules)
        {
            foreach (var tagMatchingRule in boundRulesInfo.Rules)
            {
                if (tagMatchingRule.TagStructure == TagStructure.Unspecified)
                {
                    // The current tag matching rule isn't specified so it should never be used as the resolved tag structure since it
                    // says it doesn't have an opinion.
                }
                else if (tagMatchingRule.TagStructure == TagStructure.NormalOrSelfClosing)
                {
                    // We have a rule that indicates it can be normal or self-closing, that always wins because
                    // it's all encompassing. Meaning, even if all previous rules indicate "no children" and at least
                    // one says it supports children we render the tag as having the potential to have children.
                    autoClosingBehavior = AutoClosingBehavior.EndTag;
                    return true;
                }
                else
                {
                    resolvedTagStructure = tagMatchingRule.TagStructure;
                }
            }
        }

        Debug.Assert(resolvedTagStructure != TagStructure.NormalOrSelfClosing, "Normal tag structure should already have been preferred");

        if (resolvedTagStructure == TagStructure.WithoutEndTag)
        {
            autoClosingBehavior = AutoClosingBehavior.SelfClosing;
            return true;
        }

        autoClosingBehavior = default;
        return false;
    }

    private static bool CouldAutoCloseParentOrSelf(string currentTagName, RazorSyntaxNode node)
    {
        do
        {
            string? potentialStartTagName = null;
            RazorSyntaxNode? endTag = null;
            if (node is BaseMarkupElementSyntax element)
            {
                potentialStartTagName = element.StartTag?.Name.Content ?? element.EndTag?.Name.Content;
                endTag = element.EndTag;
            }

            // Note - potentialStartTagName can be null for cases when markup element is contained in markup
            // or another non-tag structure.We skip non-tag structures and keep going up the tree.
            // In cases where a tag is surrounded by a C# statement MarkupElementSyntax will be a child of a
            // MarkupBlock, but might still be "stealing" the closing tag of an enclosing element with the
            // same tag name. E.g.
            //
            // <div>
            //     @if (true)
            //     {
            //         <div>|
            //     }
            // </div>
            // 
            // In this case, inner <div> will be parsed as a complete tag with closing </div>,
            // and the outer <div> will be missing closing tag. We need to keep going up the tree
            // until we find either tree root or a tag with the same name missing closing tag.

            if (string.Equals(potentialStartTagName, currentTagName, StringComparison.Ordinal))
            {
                // If we find a parent tag with the same name that's missing closing tag, 
                // it's likely the case above where inner "unbalanced" tag is "stealing" end tag from the
                // parent "balanced" tag. So in reality the end tag for the inner tag is likely missing,
                // thus we should insert it. E.g. in the example below
                // <div>
                //   <div>
                // </div>
                // the closing tag will be parsed as belonging to the inner <div> rather than outer, parent <div>
                // and the outer dive will be unbalanced/missing end tag. 
                if (endTag is null)
                {
                    return true;
                }

                // Has an end-tag; however, it could be another level of parent which is OK lets keep going up
            }

            // Don't stop if encountering a different tag name. When there is an unclosed inner tag
            // (normal case for auto-insert) syntax tree is pretty strange and wrapping tag with different name
            // should not stop us from going up the tree. E.g. continue going up for this case
            // <div>
            //     <blockquote>
            //         <div>|
            //     </blockquote>
            // </div>

            node = node.Parent;
        } while (node is not null);

        return false;
    }

    private enum AutoClosingBehavior
    {
        EndTag,
        SelfClosing,
    }

    private readonly record struct TagNameWithClosingBehavior(string TagName, AutoClosingBehavior AutoClosingBehavior);
}
