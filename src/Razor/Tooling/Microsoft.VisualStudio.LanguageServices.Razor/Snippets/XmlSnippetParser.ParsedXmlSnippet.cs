// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.VisualStudio.Razor.Snippets;

internal partial class XmlSnippetParser
{
    internal class ParsedXmlSnippet
    {
        public ImmutableArray<SnippetPart> Parts { get; }
        public string DefaultText { get; }

        public ParsedXmlSnippet(ImmutableArray<SnippetPart> parts)
        {
            Parts = parts;

            using var _ = StringBuilderPool.GetPooledObject(out var builder);
            foreach (var part in parts)
            {
                var textToAdd = part.DefaultText;
                builder.Append(textToAdd);
            }

            DefaultText = builder.ToString();
        }
    }

    internal abstract record SnippetPart(string DefaultText)
    {
    }

    internal record SnippetFieldPart(string FieldName, string DefaultText, int? EditIndex) : SnippetPart(DefaultText);

    internal record SnippetFunctionPart(string FieldName, string DefaultText, int? EditIndex, string FunctionName, string? FunctionParam)
        : SnippetFieldPart(FieldName, DefaultText, EditIndex)
    {
    }

    internal record SnippetCursorPart() : SnippetPart("$0")
    {
        public static SnippetCursorPart Instance = new();
    }

    internal record SnippetStringPart(string Text) : SnippetPart(Text);
    internal record SnippetShortcutPart() : SnippetPart("$shortcut$")
    {
        public static SnippetShortcutPart Instance = new();
    }
}
