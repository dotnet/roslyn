// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Xml;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Text.Adornments;

namespace Microsoft.CodeAnalysis.Remote.Razor.Tooltip;

internal static class DocCommentHelpers
{
    private static readonly char[] s_newLineChars = ['\n', '\r'];

    public static string ReduceTypeName(string content)
        => ReduceFullName(content, reduceWhenDotCount: 1);

    private static string ReduceFullName(string content, int reduceWhenDotCount)
    {
        // Starts searching backwards and then substrings everything when it finds enough dots. i.e.
        // ReduceFullName("Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName", 1) == "SomeTypeName"
        //
        // ReduceFullName("Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.AspAction", 2) == "SomeTypeName.AspAction"
        //
        // This is also smart enough to ignore nested dots in type generics[<>], methods[()], cref generics[{}].

        if (reduceWhenDotCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reduceWhenDotCount));
        }

        var dotsSeen = 0;
        var scope = 0;
        for (var i = content.Length - 1; i >= 0; i--)
        {
            do
            {
                if (content[i] == '}')
                {
                    scope++;
                }
                else if (content[i] == '{')
                {
                    scope--;
                }

                if (scope > 0)
                {
                    i--;
                }
            }
            while (scope != 0 && i >= 0);

            if (i < 0)
            {
                // Could not balance scope
                return content;
            }

            do
            {
                if (content[i] == ')')
                {
                    scope++;
                }
                else if (content[i] == '(')
                {
                    scope--;
                }

                if (scope > 0)
                {
                    i--;
                }
            }
            while (scope != 0 && i >= 0);

            if (i < 0)
            {
                // Could not balance scope
                return content;
            }

            do
            {
                if (content[i] == '>')
                {
                    scope++;
                }
                else if (content[i] == '<')
                {
                    scope--;
                }

                if (scope > 0)
                {
                    i--;
                }
            }
            while (scope != 0 && i >= 0);

            if (i < 0)
            {
                // Could not balance scope
                return content;
            }

            if (content[i] == '.')
            {
                dotsSeen++;
            }

            if (dotsSeen == reduceWhenDotCount)
            {
                var piece = content[(i + 1)..];
                return piece;
            }
        }

        // Could not reduce name
        return content;
    }

    public static bool TryExtractSummary(string? documentation, [NotNullWhen(true)] out string? summary)
    {
        const string SummaryStartTag = "<summary>";
        const string SummaryEndTag = "</summary>";

        if (documentation is null || documentation == string.Empty)
        {
            summary = null;
            return false;
        }

        var summaryTagStart = documentation.IndexOf(SummaryStartTag, StringComparison.OrdinalIgnoreCase);
        var summaryTagEndStart = documentation.IndexOf(SummaryEndTag, StringComparison.OrdinalIgnoreCase);
        if (summaryTagStart == -1 || summaryTagEndStart == -1)
        {
            // Plain-text fallback should not include line breaks that only surround the documentation.
            var trimmedDocumentation = documentation.Trim(s_newLineChars);

            // A really wrong but cheap way to check if this is XML
            if (!trimmedDocumentation.StartsWith("<", StringComparison.Ordinal) && !trimmedDocumentation.EndsWith(">", StringComparison.Ordinal))
            {
                // This doesn't look like a doc comment, we'll return it as-is.
                summary = trimmedDocumentation;
                return true;
            }

            summary = null;
            return false;
        }

        var summaryContentStart = summaryTagStart + SummaryStartTag.Length;
        var summaryContentLength = summaryTagEndStart - summaryContentStart;

        summary = documentation.Substring(summaryContentStart, summaryContentLength);
        return true;
    }

    public static ImmutableArray<TaggedText> FormatSummary(
        string summary,
        string typeName,
        string? propertyName,
        DocCommentFormattingContext formattingContext,
        CancellationToken cancellationToken)
    {
        // Descriptors can refer to framework types that aren't part of the generated compilation.
        // Roslyn can still format the XML; ObjectType only means type-parameter references stay unresolved.
        var symbol = formattingContext.ResolveSymbol(typeName, propertyName) ?? formattingContext.SemanticModel.Compilation.ObjectType;

        try
        {
            return formattingContext.FormattingService.Format(
                summary,
                symbol,
                formattingContext.SemanticModel,
                formattingContext.Position,
                ISymbolExtensions2.CrefFormat,
                typeDisplayInfo: default,
                cancellationToken);
        }
        catch (XmlException)
        {
            // TryExtractSummary passes through documentation that doesn't look like XML, and that text could still
            // contain things like a stray '<' or '&'. Better to show it as-is than fail the whole tooltip.
            return [new TaggedText(TextTags.Text, summary.Trim())];
        }
    }

    public static void AddClassifiedSummary(List<ClassifiedTextRun> runs, ImmutableArray<TaggedText> parts)
    {
        foreach (var part in parts)
        {
            var style = ClassifiedTextRunStyle.Plain;
            if (part.Style.HasFlag(TaggedTextStyle.Code))
            {
                style |= ClassifiedTextRunStyle.UseClassificationFont;
            }

            if (part.Style.HasFlag(TaggedTextStyle.Strong))
            {
                style |= ClassifiedTextRunStyle.Bold;
            }

            if (part.Style.HasFlag(TaggedTextStyle.Emphasis))
            {
                style |= ClassifiedTextRunStyle.Italic;
            }

            if (part.Style.HasFlag(TaggedTextStyle.Underline))
            {
                style |= ClassifiedTextRunStyle.Underline;
            }

            runs.Add(new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text, style));
        }
    }
}
