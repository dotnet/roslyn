// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal partial class AbstractMetadataAsSourceService
{
    internal class DocCommentFormatter
    {
        private const int s_indentSize = 2;
        private const int s_wrapLength = 80;

        private static readonly string s_indent = new(' ', s_indentSize * 2);

        private static readonly string s_summaryHeader = FeaturesResources.Summary_colon;
        private static readonly string s_paramHeader = FeaturesResources.Parameters_colon;
        private const string s_labelFormat = "{0}:";
        private static readonly string s_typeParameterHeader = FeaturesResources.Type_parameters_colon;
        private static readonly string s_returnsHeader = FeaturesResources.Returns_colon;
        private static readonly string s_valueHeader = FeaturesResources.Value_colon;
        private static readonly string s_exceptionsHeader = FeaturesResources.Exceptions_colon;
        private static readonly string s_remarksHeader = FeaturesResources.Remarks_colon;

        internal static ImmutableArray<string> Format(IDocumentationCommentFormattingService docCommentFormattingService, DocumentationComment docComment)
        {
            using var _1 = ArrayBuilder<string>.GetInstance(out var formattedCommentLinesBuilder);
            using var _2 = PooledStringBuilder.GetInstance(out var lineBuilder);

            AddWrappedTextFromRawText(
                docCommentFormattingService.Format(docComment.SummaryText),
                formattedCommentLinesBuilder,
                prefix: s_summaryHeader);

            var parameterNames = docComment.ParameterNames;
            if (parameterNames.Length > 0)
            {
                formattedCommentLinesBuilder.Add(string.Empty);
                formattedCommentLinesBuilder.Add(s_paramHeader);

                for (var i = 0; i < parameterNames.Length; i++)
                {
                    if (i != 0)
                    {
                        formattedCommentLinesBuilder.Add(string.Empty);
                    }

                    lineBuilder.Clear();
                    lineBuilder.Append(' ', s_indentSize);
                    lineBuilder.Append(string.Format(s_labelFormat, parameterNames[i]));
                    formattedCommentLinesBuilder.Add(lineBuilder.ToString());

                    AddWrappedTextFromRawText(
                        docCommentFormattingService.Format(docComment.GetParameterText(parameterNames[i])),
                        formattedCommentLinesBuilder);
                }
            }

            var typeParameterNames = docComment.TypeParameterNames;
            if (typeParameterNames.Length > 0)
            {
                formattedCommentLinesBuilder.Add(string.Empty);
                formattedCommentLinesBuilder.Add(s_typeParameterHeader);

                for (var i = 0; i < typeParameterNames.Length; i++)
                {
                    if (i != 0)
                    {
                        formattedCommentLinesBuilder.Add(string.Empty);
                    }

                    lineBuilder.Clear();
                    lineBuilder.Append(' ', s_indentSize);
                    lineBuilder.Append(string.Format(s_labelFormat, typeParameterNames[i]));
                    formattedCommentLinesBuilder.Add(lineBuilder.ToString());

                    AddWrappedTextFromRawText(
                        docCommentFormattingService.Format(docComment.GetTypeParameterText(typeParameterNames[i])),
                        formattedCommentLinesBuilder);
                }
            }

            AddWrappedTextFromRawText(
                docCommentFormattingService.Format(docComment.ReturnsText),
                formattedCommentLinesBuilder,
                prefix: s_returnsHeader);

            AddWrappedTextFromRawText(
                docCommentFormattingService.Format(docComment.ValueText),
                formattedCommentLinesBuilder,
                prefix: s_valueHeader);

            var exceptionTypes = docComment.ExceptionTypes;
            if (exceptionTypes.Length > 0)
            {
                formattedCommentLinesBuilder.Add(string.Empty);
                formattedCommentLinesBuilder.Add(s_exceptionsHeader);

                for (var i = 0; i < exceptionTypes.Length; i++)
                {
                    var rawExceptionTexts = docComment.GetExceptionTexts(exceptionTypes[i]);

                    for (var j = 0; j < rawExceptionTexts.Length; j++)
                    {
                        if (i != 0 || j != 0)
                        {
                            formattedCommentLinesBuilder.Add(string.Empty);
                        }

                        lineBuilder.Clear();
                        lineBuilder.Append(' ', s_indentSize);
                        lineBuilder.Append(string.Format(s_labelFormat, exceptionTypes[i]));
                        formattedCommentLinesBuilder.Add(lineBuilder.ToString());

                        AddWrappedTextFromRawText(docCommentFormattingService.Format(rawExceptionTexts[j]), formattedCommentLinesBuilder);
                    }
                }
            }

            AddWrappedTextFromRawText(
                docCommentFormattingService.Format(docComment.RemarksText),
                formattedCommentLinesBuilder,
                prefix: s_remarksHeader);

            // Eliminate any blank lines at the beginning.
            while (formattedCommentLinesBuilder is [{ Length: 0 }, ..])
                formattedCommentLinesBuilder.RemoveAt(0);

            // Eliminate any blank lines at the end.
            while (formattedCommentLinesBuilder is [.., { Length: 0 }])
                formattedCommentLinesBuilder.RemoveAt(formattedCommentLinesBuilder.Count - 1);

            return formattedCommentLinesBuilder.ToImmutableAndClear();
        }

        private static void AddWrappedTextFromRawText(
            string rawText, ArrayBuilder<string> result, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return;

            if (prefix != null)
            {
                if (result.Count > 0)
                    result.Add(string.Empty);

                result.Add(prefix);
            }

            // First split the string into constituent lines.
            var split = Split(rawText.AsMemory(), "\r\n".AsSpan());

            // Now split each line into multiple lines.
            foreach (var item in split)
                SplitRawLineIntoFormattedLines(item, result);
        }

        private static void SplitRawLineIntoFormattedLines(
            ReadOnlyMemory<char> line, ArrayBuilder<string> formattedLines)
        {
            var firstInLine = true;

            using var _ = PooledStringBuilder.GetInstance(out var sb);

            var span = line.Span;

            while (span.Length > 0)
            {
                span = span.TrimStart(' ');

                if (span.Length == 0)
                    break;

                // We must always append at least one word to ensure progress.
                if (firstInLine)
                {
                    firstInLine = false;
                    sb.Append(s_indent);
                }
                else
                {
                    sb.Append(' ');
                }

                var end = span.IndexOf(" ".AsSpan());

                if (end < 0)
                    end = span.Length;

                foreach (var c in span.Slice(0, end))
                    sb.Append(c);

                if (sb.Length >= s_wrapLength)
                {
                    formattedLines.Add(sb.ToString());
                    sb.Clear();
                    firstInLine = true;
                }

                span = span.Slice(end);
            }

            formattedLines.Add(sb.ToString());
        }

        public static ImmutableArray<ReadOnlyMemory<char>> Split(
            ReadOnlyMemory<char> source,
            ReadOnlySpan<char> separator)
        {
            var result = ArrayBuilder<ReadOnlyMemory<char>>.GetInstance();

            var index = source.Span.IndexOf(separator);
            while (index >= 0)
            {
                var line = source.Slice(0, index);
                result.Add(line);

                source = source.Slice(index + separator.Length);
                index = source.Span.IndexOf(separator);
            }

            if (source.Length > 0)
                result.Add(source);

            return result.ToImmutableAndFree();
        }
    }
}
