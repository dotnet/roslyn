// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Test.Utilities
{
    /// <summary>
    /// To aid with testing, we define a special type of text file that can encode additional
    /// information in it.  This prevents a test writer from having to carry around multiple sources
    /// of information that must be reconstituted.  For example, instead of having to keep around the
    /// contents of a file *and* and the location of the cursor, the tester can just provide a
    /// string with the "$" character in it.  This allows for easy creation of "FIT" tests where all
    /// that needs to be provided are strings that encode every bit of state necessary in the string
    /// itself.
    /// 
    /// The current set of encoded features we support are: 
    /// 
    /// $$ - The position in the file.  There can be at most one of these.
    /// 
    /// [| ... |] - A span of text in the file.  There can be many of these and they can be nested
    /// and/or overlap the $ position.
    /// 
    /// {|Name: ... |} A span of text in the file annotated with an identifier.  There can be many of
    /// these, including ones with the same name.
    /// 
    /// Additional encoded features can be added on a case by case basis.
    /// </summary>
    public static class MarkupTestFile
    {
        private const string PositionString = "$$";
        private const string SpanStartString = "[|";
        private const string SpanEndString = "|]";
        private const string NamedSpanStartString = "{|";
        private const string NamedSpanEndString = "|}";

        private static readonly Regex s_namedSpanStartRegex = new Regex(@"\{\| ([-_.A-Za-z0-9]+) \:",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        private static void Parse(string input, out string output, out int? position, out IDictionary<string, IList<TextSpan>> spans)
        {
            position = null;
            spans = new Dictionary<string, IList<TextSpan>>();

            var outputBuilder = new StringBuilder();

            var currentIndexInInput = 0;
            var inputOutputOffset = 0;

            // A stack of span starts along with their associated annotation name.  [||] spans simply
            // have empty string for their annotation name.
            var spanStartStack = new Stack<(int index, string value)>();

            while (true)
            {
                var matches = new List<(int index, string value)>();
                AddMatch(input, PositionString, currentIndexInInput, matches);
                AddMatch(input, SpanStartString, currentIndexInInput, matches);
                AddMatch(input, SpanEndString, currentIndexInInput, matches);
                AddMatch(input, NamedSpanEndString, currentIndexInInput, matches);

                Match namedSpanStartMatch = s_namedSpanStartRegex.Match(input, currentIndexInInput);
                if (namedSpanStartMatch.Success)
                {
                    matches.Add((namedSpanStartMatch.Index, namedSpanStartMatch.Value));
                }

                if (matches.Count == 0)
                {
                    // No more markup to process.
                    break;
                }

                var orderedMatches = matches.OrderBy(match => match.index).ToList();
                if (orderedMatches.Count >= 2 &&
                    spanStartStack.Count > 0 &&
                    matches[0].index == matches[1].index - 1)
                {
                    // We have a slight ambiguity with cases like these:
                    //
                    // [|]    [|}
                    //
                    // Is it starting a new match, or ending an existing match.  As a workaround, we
                    // special case these and consider it ending a match if we have something on the
                    // stack already.
#pragma warning disable CA1508 // Avoid dead conditional code - https://github.com/dotnet/roslyn-analyzers/issues/2180
                    if ((matches[0].value == SpanStartString && matches[1].value == SpanEndString && spanStartStack.Peek().value.Length == 0) ||
                        (matches[0].value == SpanStartString && matches[1].value == NamedSpanEndString && spanStartStack.Peek().value.Length > 0))
                    {
                        orderedMatches.RemoveAt(0);
                    }
#pragma warning restore CA1508 // Avoid dead conditional code
                }

                // Order the matches by their index
                var (matchIndexInInput, matchString) = orderedMatches.First();

                int matchIndexInOutput = matchIndexInInput - inputOutputOffset;
                outputBuilder.Append(input.Substring(currentIndexInInput, matchIndexInInput - currentIndexInInput));

                currentIndexInInput = matchIndexInInput + matchString.Length;
                inputOutputOffset += matchString.Length;

                switch (matchString.Substring(0, 2))
                {
                    case PositionString:
                        if (position.HasValue)
                        {
                            throw new ArgumentException(string.Format("Saw multiple occurrences of {0}", PositionString));
                        }

                        position = matchIndexInOutput;
                        break;

                    case SpanStartString:
                        spanStartStack.Push((matchIndexInOutput, string.Empty));
                        break;

                    case SpanEndString:
                        if (spanStartStack.Count == 0)
                        {
                            throw new ArgumentException(string.Format("Saw {0} without matching {1}", SpanEndString, SpanStartString));
                        }

                        if (spanStartStack.Peek().value.Length > 0)
                        {
                            throw new ArgumentException(string.Format("Saw {0} without matching {1}", NamedSpanStartString, NamedSpanEndString));
                        }

                        PopSpan(spanStartStack, spans, matchIndexInOutput);
                        break;

                    case NamedSpanStartString:
                        string name = namedSpanStartMatch.Groups[1].Value;
                        spanStartStack.Push((matchIndexInOutput, name));
                        break;

                    case NamedSpanEndString:
                        if (spanStartStack.Count == 0)
                        {
                            throw new ArgumentException(string.Format("Saw {0} without matching {1}", NamedSpanEndString, NamedSpanStartString));
                        }

                        if (spanStartStack.Peek().value.Length == 0)
                        {
                            throw new ArgumentException(string.Format("Saw {0} without matching {1}", SpanStartString, SpanEndString));
                        }

                        PopSpan(spanStartStack, spans, matchIndexInOutput);
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            if (spanStartStack.Count > 0)
            {
                throw new ArgumentException(string.Format("Saw {0} without matching {1}", SpanStartString, SpanEndString));
            }

            // Append the remainder of the string.
            outputBuilder.Append(input.Substring(currentIndexInInput));
            output = outputBuilder.ToString();
        }

        private static V GetOrAdd<K, V>(IDictionary<K, V> dictionary, K key, Func<K, V> function)
        {
            if (!dictionary.TryGetValue(key, out V value))
            {
                value = function(key);
                dictionary.Add(key, value);
            }

            return value;
        }

        private static void PopSpan(
            Stack<(int index, string value)> spanStartStack,
            IDictionary<string, IList<TextSpan>> spans,
            int finalIndex)
        {
            var (index, value) = spanStartStack.Pop();

            TextSpan span = TextSpan.FromBounds(index, finalIndex);
            GetOrAdd(spans, value, _ => new List<TextSpan>()).Add(span);
        }

        private static void AddMatch(string input, string value, int currentIndex, List<(int index, string value)> matches)
        {
            int index = input.IndexOf(value, currentIndex, StringComparison.Ordinal);
            if (index >= 0)
            {
                matches.Add((index, value));
            }
        }

        private static void GetPositionAndSpans(string input, out string output, out int? cursorPositionOpt, out IList<TextSpan> spans)
        {
            Parse(input, out output, out cursorPositionOpt, out IDictionary<string, IList<TextSpan>> dictionary);

            spans = GetOrAdd(dictionary, string.Empty, _ => new List<TextSpan>());
        }

        public static void GetPositionAndSpan(string input, out string output, out int? cursorPosition, out TextSpan? textSpan)
        {
            GetPositionAndSpans(input, out output, out cursorPosition, out IList<TextSpan> spans);

            textSpan = spans.Count == 0 ? null : (TextSpan?)spans.Single();
        }
    }
}
