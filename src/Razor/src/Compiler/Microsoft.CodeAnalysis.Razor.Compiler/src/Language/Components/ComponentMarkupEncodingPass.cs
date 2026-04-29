// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentMarkupEncodingPass(RazorLanguageVersion version) : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    private readonly RazorLanguageVersion _version = version;

    // Runs after ComponentMarkupBlockPass
    public override int Order => ComponentMarkupDiagnosticPass.DefaultOrder + 20;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        if (documentNode.Options.DesignTime)
        {
            // Nothing to do during design time.
            return;
        }

        var rewriter = new Rewriter(_version);
        rewriter.Visit(documentNode);
    }

    private sealed class Rewriter(RazorLanguageVersion version) : IntermediateNodeWalker
    {
        // Markup content in components are rendered in one of the following two ways:
        //
        // 1. AddContent - we encode it when used with pre-rendering and inserted into the DOM in a safe way (low perf impact)
        // 2. AddMarkupContent - renders the content directly as markup (high perf impact)
        //
        // Because of this, we want to use AddContent as much as possible.
        //
        // We want to use AddMarkupContent to avoid aggressive encoding during pre-rendering.
        // Specifically, when one of the following characters are in the content:
        //
        // 1. New lines (\r, \n), tabs (\t), angle brackets (<, >) - so they get rendered as actual new lines, tabs, brackets instead of &#xA;
        // 2. Any character outside the ASCII range

        private static readonly FrozenSet<char> EncodedCharacters = ['\r', '\n', '\t', '<', '>'];

        private readonly bool _avoidEncodingScripts = version >= RazorLanguageVersion.Version_8_0;

        private bool _avoidEncodingContent;

        public override void VisitMarkupElement(MarkupElementIntermediateNode node)
        {
            // We don't want to HTML-encode literal content inside <script> tags.
            var oldAvoidEncodingContent = _avoidEncodingContent;

            _avoidEncodingContent = _avoidEncodingContent || (_avoidEncodingScripts && IsScript(node));

            try
            {
                base.VisitMarkupElement(node);
            }
            finally
            {
                _avoidEncodingContent = oldAvoidEncodingContent;
            }

            static bool IsScript(MarkupElementIntermediateNode node)
            {
                return string.Equals("script", node.TagName, StringComparison.OrdinalIgnoreCase);
            }
        }

        public override void VisitHtml(HtmlContentIntermediateNode node)
        {
            if (_avoidEncodingContent)
            {
                node.HasEncodedContent = true;
                return;
            }

            // We'll count any ampersand ('&') characters we find.
            var ampersandCount = 0;

            foreach (var child in node.Children)
            {
                if (child is not HtmlIntermediateToken token || token.Content.IsNullOrEmpty())
                {
                    // We only care about Html tokens.
                    continue;
                }

                foreach (var ch in token.Content)
                {
                    // ASCII range is 0 - 127
                    if (ch > 127 || EncodedCharacters.Contains(ch))
                    {
                        node.HasEncodedContent = true;
                        return;
                    }

                    if (ch == '&')
                    {
                        ampersandCount++;
                    }
                }
            }

            // If we reach here, we don't have new-lines, tabs or non-ascii characters in this node.

            // if there aren't any ampersands, we know there aren't any HTML character entities to decode.
            if (ampersandCount == 0)
            {
                return;
            }

            // Use ampersand count as a capacity hint. We double the count because text is likely present
            // after each entity and add 1 for any text before the first entity.
            using var toUpdate = new PooledArrayBuilder<(HtmlIntermediateToken token, string content)>(capacity: (ampersandCount * 2) + 1);

            foreach (var child in node.Children)
            {
                if (child is not HtmlIntermediateToken token || token.Content.IsNullOrEmpty())
                {
                    // We only care about Html tokens.
                    continue;
                }

                if (TryDecodeHtmlEntities(token.Content.AsMemory(), out var decoded))
                {
                    toUpdate.Add((token, decoded));
                }
                else
                {
                    node.HasEncodedContent = true;
                    return;
                }
            }

            // If we reach here, it means we have successfully decoded all content.
            // Replace all token content with the decoded value.
            foreach (var (token, content) in toUpdate)
            {
                token.UpdateContent(content);
            }
        }

        private static bool TryDecodeHtmlEntities(ReadOnlyMemory<char> content, [NotNullWhen(true)] out string? decoded)
        {
            decoded = null;

            if (content.IsEmpty)
            {
                decoded = string.Empty;
                return true;
            }

            decoded = string.TryBuild(content, static (ref builder, content) =>
            {
                while (!content.IsEmpty)
                {
                    var ampersandIndex = content.Span.IndexOf('&');

                    if (ampersandIndex == -1)
                    {
                        // No more entities, add the remaining content
                        builder.Append(content);
                        break;
                    }

                    if (!ParserHelpers.TryGetHtmlEntity(content[ampersandIndex..], out var entity, out var replacement))
                    {
                        // We found a '&' that we don't know what to do with. Don't try to decode further.
                        return false;
                    }

                    // We found a valid entity.
                    // First, add the text before the entity.
                    // Then, add the replacement text for the entity.
                    if (ampersandIndex > 0)
                    {
                        builder.Append(content[..ampersandIndex]);
                    }

                    builder.Append(replacement.AsMemory());

                    // Skip past the processed entity and continue.
                    content = content[(ampersandIndex + entity.Length)..];
                }

                Debug.Assert(builder.Length > 0, "How could builder be empty if content was not?");

                return true;
            });

            return decoded is not null;
        }
    }
}
