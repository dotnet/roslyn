// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        internal class DocCommentFormatter
        {
            private const int s_indentSize = 2;
            private const int s_wrapLength = 80;

            private static readonly string s_summaryHeader = FeaturesResources.Summary_colon;
            private static readonly string s_paramHeader = FeaturesResources.Parameters_colon;
            private const string s_labelFormat = "{0}:";
            private static readonly string s_typeParameterHeader = FeaturesResources.Type_parameters_colon;
            private static readonly string s_returnsHeader = FeaturesResources.Returns_colon;
            private static readonly string s_exceptionsHeader = FeaturesResources.Exceptions_colon;
            private static readonly string s_remarksHeader = FeaturesResources.Remarks_colon;

            internal static ImmutableArray<string> Format(IDocumentationCommentFormattingService docCommentFormattingService, DocumentationComment docComment)
            {
                var formattedCommentLinesBuilder = ArrayBuilder<string>.GetInstance();
                var lineBuilder = new StringBuilder();

                var formattedSummaryText = docCommentFormattingService.Format(docComment.SummaryText);
                if (!string.IsNullOrWhiteSpace(formattedSummaryText))
                {
                    formattedCommentLinesBuilder.Add(s_summaryHeader);
                    formattedCommentLinesBuilder.AddRange(CreateWrappedTextFromRawText(formattedSummaryText));
                }

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

                        var rawParameterText = docComment.GetParameterText(parameterNames[i]);
                        var formattedParameterText = docCommentFormattingService.Format(rawParameterText);
                        if (!string.IsNullOrWhiteSpace(formattedParameterText))
                        {
                            formattedCommentLinesBuilder.AddRange(CreateWrappedTextFromRawText(formattedParameterText));
                        }
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

                        var rawTypeParameterText = docComment.GetTypeParameterText(typeParameterNames[i]);
                        var formattedTypeParameterText = docCommentFormattingService.Format(rawTypeParameterText);
                        if (!string.IsNullOrWhiteSpace(formattedTypeParameterText))
                        {
                            formattedCommentLinesBuilder.AddRange(CreateWrappedTextFromRawText(formattedTypeParameterText));
                        }
                    }
                }

                var formattedReturnsText = docCommentFormattingService.Format(docComment.ReturnsText);
                if (!string.IsNullOrWhiteSpace(formattedReturnsText))
                {
                    formattedCommentLinesBuilder.Add(string.Empty);
                    formattedCommentLinesBuilder.Add(s_returnsHeader);
                    formattedCommentLinesBuilder.AddRange(CreateWrappedTextFromRawText(formattedReturnsText));
                }

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

                            var formattedExceptionText = docCommentFormattingService.Format(rawExceptionTexts[j]);
                            if (!string.IsNullOrWhiteSpace(formattedExceptionText))
                            {
                                formattedCommentLinesBuilder.AddRange(CreateWrappedTextFromRawText(formattedExceptionText));
                            }
                        }
                    }
                }

                var formattedRemarksText = docCommentFormattingService.Format(docComment.RemarksText);
                if (!string.IsNullOrWhiteSpace(formattedRemarksText))
                {
                    formattedCommentLinesBuilder.Add(string.Empty);
                    formattedCommentLinesBuilder.Add(s_remarksHeader);
                    formattedCommentLinesBuilder.AddRange(CreateWrappedTextFromRawText(formattedRemarksText));
                }

                // Eliminate any blank lines at the beginning.
                while (formattedCommentLinesBuilder.Count > 0 &&
                       formattedCommentLinesBuilder[0].Length == 0)
                {
                    formattedCommentLinesBuilder.RemoveAt(0);
                }

                // Eliminate any blank lines at the end.
                while (formattedCommentLinesBuilder.Count > 0 &&
                       formattedCommentLinesBuilder[formattedCommentLinesBuilder.Count - 1].Length == 0)
                {
                    formattedCommentLinesBuilder.RemoveAt(formattedCommentLinesBuilder.Count - 1);
                }

                return formattedCommentLinesBuilder.ToImmutableAndFree();
            }

            private static ImmutableArray<string> CreateWrappedTextFromRawText(string rawText)
            {
                var lines = ArrayBuilder<string>.GetInstance();

                // First split the string into constituent lines.
                var split = rawText.Split(new[] { "\r\n" }, System.StringSplitOptions.None);

                // Now split each line into multiple lines.
                foreach (var item in split)
                {
                    SplitRawLineIntoFormattedLines(item, lines);
                }

                return lines.ToImmutableAndFree();
            }

            private static void SplitRawLineIntoFormattedLines(
                string line, ArrayBuilder<string> lines)
            {
                var indent = new StringBuilder().Append(' ', s_indentSize * 2).ToString();

                var words = line.Split(' ');
                var firstInLine = true;

                var sb = new StringBuilder();
                sb.Append(indent);
                foreach (var word in words)
                {
                    // We must always append at least one word to ensure progress.
                    if (firstInLine)
                    {
                        firstInLine = false;
                    }
                    else
                    {
                        sb.Append(' ');
                    }

                    sb.Append(word);

                    if (sb.Length >= s_wrapLength)
                    {
                        lines.Add(sb.ToString());
                        sb.Clear();
                        sb.Append(indent);
                        firstInLine = true;
                    }
                }

                if (sb.ToString().Trim() != string.Empty)
                {
                    lines.Add(sb.ToString());
                }
            }
        }
    }
}
