// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.Razor;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Remote.Razor.Completion;

/// <summary>
/// Recognizes the positions where <c>~/</c> asset paths are meaningful: inside the value of an
/// attribute that opted into asset-path expansion, on a value that starts with <c>~/</c>.
/// </summary>
/// <remarks>
/// Shared by the completion provider and by the decision to suppress the HTML language server's
/// relative-path completions, so the two cannot disagree about where an asset path is being typed.
/// The opt-in rules mirror <c>ComponentTildePathPass</c>: an allowlisted <c>(element, attribute)</c>
/// pair from <c>[AcceptsAssetPath]</c>, or a component parameter marked <c>[AssetPath]</c>.
/// </remarks>
internal static class AssetPathCompletionFacts
{
    public const string TildePrefix = "~/";

    /// <summary>
    /// Whether asset path expansion can happen in this document at all. The tag helper producer that
    /// discovers <c>[AcceptsAssetPath]</c> is registered from Razor 3.0, but
    /// <c>ComponentTildePathPass</c> only expands from 11.0, so a project referencing a modern
    /// runtime while pinned to an older language version has the opt-in metadata and no expansion.
    /// Offering completions there would promise a rewrite that never happens.
    /// </summary>
    public static bool IsSupported(RazorParserOptions options)
        => options.FileKind.IsComponent() &&
           options.LanguageVersion >= RazorLanguageVersion.Version_11_0;

    /// <summary>
    /// A purely textual test for whether the position could be inside a <c>~/</c> attribute value,
    /// used to decide whether the project-scoped asset data is worth resolving at all. Scans back to
    /// the opening quote, so it costs nothing on the overwhelming majority of keystrokes that are
    /// nowhere near an asset path.
    /// </summary>
    public static bool IsAssetPathCandidate(SourceText sourceText, int absoluteIndex)
    {
        if (absoluteIndex < 0 || absoluteIndex > sourceText.Length)
        {
            return false;
        }

        for (var i = absoluteIndex - 1; i >= 0; i--)
        {
            var c = sourceText[i];

            if (c is '"' or '\'')
            {
                return i + 2 < sourceText.Length &&
                       sourceText[i + 1] == '~' &&
                       sourceText[i + 2] == '/';
            }

            // An attribute value can't span these, so anything beyond one is a different construct.
            if (c is '<' or '>' or '\r' or '\n')
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether <paramref name="absoluteIndex"/> sits in an opted-in attribute value that
    /// begins with <c>~/</c>, and if so returns the span running from just after the prefix to the
    /// end of the value, which is the text a completion item replaces.
    /// </summary>
    public static bool TryGetAssetPathContext(
        RazorSyntaxNode? owner,
        AssetPathCompletionInfo info,
        SourceText sourceText,
        int absoluteIndex,
        out TextSpan replacementSpan)
    {
        replacementSpan = default;

        if (info.IsEmpty ||
            FindAttribute(owner) is not { } attribute ||
            !IsOptedIn(attribute, info))
        {
            return false;
        }

        var valueSpan = GetValueSpan(attribute);
        if (!valueSpan.IntersectsWith(absoluteIndex) ||
            valueSpan.Length < TildePrefix.Length ||
            sourceText.ToString(new TextSpan(valueSpan.Start, TildePrefix.Length)) != TildePrefix)
        {
            return false;
        }

        if (!IsStaticLiteral(attribute))
        {
            // Expansion only applies to a value that is entirely static text, so offering an asset
            // here would suggest something the compiler leaves as a literal and diagnoses.
            return false;
        }

        var pathStart = valueSpan.Start + TildePrefix.Length;
        if (absoluteIndex < pathStart)
        {
            // The cursor is on the '~' or the '/' itself, so there is no path being typed yet.
            return false;
        }

        replacementSpan = TextSpan.FromBounds(pathStart, valueSpan.End);
        return true;
    }

    /// <summary>
    /// Walks out to the attribute containing the position, stopping at the enclosing element so an
    /// index that is in no attribute value doesn't pick up an unrelated attribute further out.
    /// </summary>
    private static RazorSyntaxNode? FindAttribute(RazorSyntaxNode? owner)
    {
        for (var node = owner; node is not null; node = node.Parent)
        {
            switch (node)
            {
                case MarkupAttributeBlockSyntax:
                case MarkupTagHelperAttributeSyntax:
                    return node;

                case MarkupElementSyntax:
                case MarkupTagHelperElementSyntax:
                    return null;
            }
        }

        return null;
    }

    private static bool IsOptedIn(RazorSyntaxNode attribute, AssetPathCompletionInfo info)
        => attribute switch
        {
            MarkupAttributeBlockSyntax attributeBlock
                => GetElementName(attributeBlock) is { } elementName &&
                   info.AcceptsAssetPath(elementName, attributeBlock.Name.GetContent()),

            MarkupTagHelperAttributeSyntax tagHelperAttribute
                => AcceptsAssetPath(tagHelperAttribute),

            _ => false
        };

    private static bool AcceptsAssetPath(MarkupTagHelperAttributeSyntax attribute)
    {
        if (attribute is not { Parent.Parent: MarkupTagHelperElementSyntax { TagHelperInfo.BindingResult: var binding } })
        {
            return false;
        }

        var attributeName = attribute.TagHelperAttributeInfo.Name;

        foreach (var tagHelper in binding.TagHelpers)
        {
            foreach (var boundAttribute in tagHelper.BoundAttributes)
            {
                if (string.Equals(boundAttribute.Name, attributeName, StringComparison.Ordinal) &&
                    boundAttribute.Metadata is PropertyMetadata { AcceptsAssetPath: true })
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Whether the attribute's value is entirely static text. A value that mixes a literal with a
    /// C# expression never expands, so the '@' transition is what disqualifies it.
    /// </summary>
    private static bool IsStaticLiteral(RazorSyntaxNode attribute)
    {
        var value = attribute switch
        {
            MarkupAttributeBlockSyntax a => (RazorSyntaxNode?)a.Value,
            MarkupTagHelperAttributeSyntax a => a.Value,
            _ => null
        };

        if (value is null)
        {
            return true;
        }

        foreach (var descendant in value.DescendantNodes())
        {
            if (descendant is CSharpSyntaxNode or MarkupDynamicAttributeValueSyntax)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The span between the quotes. An attribute whose value hasn't been typed yet has no value
    /// node, so the span is empty but still positioned where the text will go.
    /// </summary>
    private static TextSpan GetValueSpan(RazorSyntaxNode attribute)
    {
        var (value, valuePrefix, valueSuffix) = attribute switch
        {
            MarkupAttributeBlockSyntax a => ((RazorSyntaxNode?)a.Value, a.ValuePrefix, a.ValueSuffix),
            MarkupTagHelperAttributeSyntax a => (a.Value, a.ValuePrefix, a.ValueSuffix),
            _ => (null, null, null)
        };

        if (value is not null)
        {
            return value.Span;
        }

        if (valuePrefix is not null)
        {
            return new TextSpan(valuePrefix.Span.End, 0);
        }

        return valueSuffix is not null ? new TextSpan(valueSuffix.Span.Start, 0) : default;
    }

    private static string? GetElementName(RazorSyntaxNode attribute)
        => HtmlFacts.TryGetElementInfo(attribute.Parent, out var containingTagNameToken, out _, out _)
            ? containingTagNameToken.Content
            : null;
}
