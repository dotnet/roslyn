// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.QuickInfo;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal static class ConversionHelpers
{
    public static Uri CreateAbsoluteUri(string absolutePath)
        => ProtocolConversions.CreateAbsoluteUri(absolutePath);

    public static (string content, bool isMarkdown) CreateMarkdownContent(TextDocument document, QuickInfoItem info, XamlRequestContext context)
    {
        var clientSupportsMarkdown = context.ClientCapabilities?.TextDocument?.Hover?.ContentFormat?.Contains(LSP.MarkupKind.Markdown) == true;

        // Insert line breaks in between sections to ensure we get double spacing between sections.
        var tags = info.Sections
            .SelectMany(section => section.TaggedParts.Add(new TaggedText(TextTags.LineBreak, Environment.NewLine)))
            .ToImmutableArray();

        var language = document.Project.Language;
        var markupContent = ProtocolConversions.GetDocumentationMarkupContent(tags, language, clientSupportsMarkdown);
        return (markupContent.Value, markupContent.Kind == LSP.MarkupKind.Markdown);
    }
}
