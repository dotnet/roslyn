﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal static class FormattingExtensions
    {
        public static SyntaxNode GetParentWithBiggerSpan(this SyntaxNode node)
        {
            if (node.Parent == null)
            {
                return node;
            }

            if (node.Parent.Span != node.Span)
            {
                return node.Parent;
            }

            return GetParentWithBiggerSpan(node.Parent);
        }

        public static IEnumerable<IFormattingRule> Concat(this IFormattingRule rule, IEnumerable<IFormattingRule> rules)
        {
            return SpecializedCollections.SingletonEnumerable(rule).Concat(rules);
        }

        public static void AddRange<T>(this IList<T> list, IEnumerable<T> values)
        {
            foreach (var v in values)
            {
                list.Add(v);
            }
        }

        public static List<T> Combine<T>(this List<T> list1, List<T> list2)
        {
            if (list1 == null)
            {
                return list2;
            }
            else if (list2 == null)
            {
                return list1;
            }

            // normal case
            var combinedList = new List<T>(list1);
            combinedList.AddRange(list2);

            return combinedList;
        }

        public static bool ContainsElasticTrivia(this SuppressOperation operation, TokenStream tokenStream)
        {
            var startToken = tokenStream.GetTokenData(operation.StartToken);
            var nextToken = startToken.GetNextTokenData();
            var endToken = tokenStream.GetTokenData(operation.EndToken);
            var previousToken = endToken.GetPreviousTokenData();

            return tokenStream.GetTriviaData(startToken, nextToken).TreatAsElastic || tokenStream.GetTriviaData(previousToken, endToken).TreatAsElastic;
        }

        public static bool HasAnyWhitespaceElasticTrivia(this SyntaxTriviaList list)
        {
            // Use foreach to avoid accessing indexer as it will call GetSlotOffset for each trivia
            foreach (var trivia in list)
            {
                if (trivia.IsElastic())
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsOn(this IndentBlockOption option, IndentBlockOption flag)
        {
            return (option & flag) == flag;
        }

        public static bool IsMaskOn(this IndentBlockOption option, IndentBlockOption mask)
        {
            return (option & mask) != 0x0;
        }

        public static bool IsOn(this SuppressOption option, SuppressOption flag)
        {
            return (option & flag) == flag;
        }

        public static bool IsMaskOn(this SuppressOption option, SuppressOption mask)
        {
            return (option & mask) != 0x0;
        }

        public static SuppressOption RemoveFlag(this SuppressOption option, SuppressOption flag)
        {
            return option & ~flag;
        }

        public static string CreateIndentationString(this int desiredIndentation, bool useTab, int tabSize)
        {
            int numberOfTabs = 0;
            int numberOfSpaces = Math.Max(0, desiredIndentation);

            if (useTab)
            {
                numberOfTabs = desiredIndentation / tabSize;
                numberOfSpaces -= numberOfTabs * tabSize;
            }

            return new string('\t', numberOfTabs) + new string(' ', numberOfSpaces);
        }

        public static StringBuilder AppendIndentationString(this StringBuilder sb, int desiredIndentation, bool useTab, int tabSize)
        {
            int numberOfTabs = 0;
            int numberOfSpaces = Math.Max(0, desiredIndentation);

            if (useTab)
            {
                numberOfTabs = desiredIndentation / tabSize;
                numberOfSpaces -= numberOfTabs * tabSize;
            }

            return sb.Append('\t', repeatCount: numberOfTabs).Append(' ', repeatCount: numberOfSpaces);
        }

        public static void ProcessTextBetweenTokens(
            this string text,
            TreeData treeInfo,
            SyntaxToken baseToken,
            int tabSize,
            out int lineBreaks,
            out int spaceOrIndentation)
        {
            // initialize out param
            lineBreaks = text.GetNumberOfLineBreaks();

            // multiple line case
            if (lineBreaks > 0)
            {
                var indentationString = text.GetLastLineText();
                spaceOrIndentation = indentationString.GetColumnFromLineOffset(indentationString.Length, tabSize);
                return;
            }

            // with tab, more expensive way. get column of token1 and then calculate right space amount
            var initialColumn = baseToken.RawKind == 0 ? 0 /* the very beginning of the file */ : treeInfo.GetOriginalColumn(tabSize, baseToken);
            spaceOrIndentation = text.ConvertTabToSpace(tabSize, baseToken.ToString().GetTextColumn(tabSize, initialColumn), text.Length);
        }

        private static readonly char[] s_trimChars = new char[] { '\r', '\n' };

        public static string AdjustIndentForXmlDocExteriorTrivia(
            this string triviaText,
            bool forceIndentation,
            int indentation,
            int indentationDelta,
            bool useTab,
            int tabSize)
        {
            var isEmptyString = false;
            var builder = StringBuilderPool.Allocate();

            var trimmedTriviaText = triviaText.TrimEnd(s_trimChars);
            var nonWhitespaceCharIndex = GetFirstNonWhitespaceIndexInString(triviaText);
            if (nonWhitespaceCharIndex == -1)
            {
                isEmptyString = true;
                nonWhitespaceCharIndex = triviaText.Length;
            }

            var newIndentation = GetNewIndentationForComments(triviaText, nonWhitespaceCharIndex, forceIndentation, indentation, indentationDelta, tabSize);

            builder.AppendIndentationString(newIndentation, useTab, tabSize);
            if (!isEmptyString)
            {
                builder.Append(triviaText, nonWhitespaceCharIndex, triviaText.Length - nonWhitespaceCharIndex);
            }

            return StringBuilderPool.ReturnAndFree(builder);
        }

        public static string ReindentStartOfXmlDocumentationComment(
            this string triviaText,
            bool forceIndentation,
            int indentation,
            int indentationDelta,
            bool useTab,
            int tabSize,
            string newLine)
        {
            var builder = StringBuilderPool.Allocate();

            // split xml doc comments into lines
            var lines = triviaText.Split('\n');
            Contract.ThrowIfFalse(lines.Length > 0);

            // add first line and append new line iff it is not a single line xml doc comment
            builder.Append(lines[0].Trim(s_trimChars));
            if (0 < lines.Length - 1)
            {
                builder.Append(newLine);
            }

            // add rest of xml doc comments
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd(s_trimChars);
                var nonWhitespaceCharIndex = GetFirstNonWhitespaceIndexInString(line);
                if (nonWhitespaceCharIndex >= 0)
                {
                    var newIndentation = GetNewIndentationForComments(line, nonWhitespaceCharIndex, forceIndentation, indentation, indentationDelta, tabSize);
                    builder.AppendIndentationString(newIndentation, useTab, tabSize);
                    builder.Append(line, nonWhitespaceCharIndex, line.Length - nonWhitespaceCharIndex);
                }

                if (i < lines.Length - 1)
                {
                    builder.Append(newLine);
                }
            }

            return StringBuilderPool.ReturnAndFree(builder);
        }

        private static int GetNewIndentationForComments(this string line, int nonWhitespaceCharIndex, bool forceIndentation, int indentation, int indentationDelta, int tabSize)
        {
            if (forceIndentation)
            {
                return indentation;
            }

            var currentIndentation = line.GetColumnFromLineOffset(nonWhitespaceCharIndex, tabSize);
            return Math.Max(currentIndentation + indentationDelta, 0);
        }

        public static int GetFirstNonWhitespaceIndexInString(this string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != ' ' && text[i] != '\t')
                {
                    return i;
                }
            }

            return -1;
        }

        public static TextChange SimpleDiff(this TextChange textChange, string text)
        {
            var span = textChange.Span;
            var newText = textChange.NewText;

            int i = 0;
            for (; i < span.Length; i++)
            {
                if (i >= newText.Length || text[i] != newText[i])
                {
                    break;
                }
            }

            // two texts are exactly same
            if (i == span.Length && text.Length == newText.Length)
            {
                // don't do anything
                return textChange;
            }

            if (i > 0)
            {
                span = new TextSpan(span.Start + i, span.Length - i);
                newText = newText.Substring(i);
            }

            return new TextChange(span, newText);
        }
    }
}
