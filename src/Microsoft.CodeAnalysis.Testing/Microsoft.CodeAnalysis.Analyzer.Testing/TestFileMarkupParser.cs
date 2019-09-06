// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
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
    public static class TestFileMarkupParser
    {
        private const string PositionString = "$$";
        private const string SpanStartString = "[|";
        private const string SpanEndString = "|]";
        private const string NamedSpanStartString = "{|";
        private const string NamedSpanEndString = "|}";

        private static readonly Regex s_namedSpanStartRegex = new Regex(
            @"\{\| ([^:|[\]{}]+) \:",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        private static void Parse(string input, out string output, out IList<int> positions, out IDictionary<string, IList<TextSpan>> spans)
        {
            positions = new List<int>();
            spans = new Dictionary<string, IList<TextSpan>>();

            var outputBuilder = new StringBuilder();

            var currentIndexInInput = 0;
            var inputOutputOffset = 0;

            // A stack of span starts along with their associated annotation name.  [||] spans simply
            // have empty string for their annotation name.
            var spanStartStack = new Stack<Tuple<int, string>>();

            while (true)
            {
                var matches = new List<(int, string)>();
                AddMatch(input, PositionString, currentIndexInInput, matches);
                AddMatch(input, SpanStartString, currentIndexInInput, matches);
                AddMatch(input, SpanEndString, currentIndexInInput, matches);
                AddMatch(input, NamedSpanEndString, currentIndexInInput, matches);

                var namedSpanStartMatch = s_namedSpanStartRegex.Match(input, currentIndexInInput);
                if (namedSpanStartMatch.Success)
                {
                    matches.Add((namedSpanStartMatch.Index, namedSpanStartMatch.Value));
                }

                if (matches.Count == 0)
                {
                    // No more markup to process.
                    break;
                }

                var orderedMatches = matches.OrderBy(t => t.Item1).ToList();
                if (orderedMatches.Count >= 2 &&
                    spanStartStack.Count > 0 &&
                    matches[0].Item1 == matches[1].Item1 - 1)
                {
                    // We have a slight ambiguity with cases like these:
                    //
                    // [|]    [|}
                    //
                    // Is it starting a new match, or ending an existing match.  As a workaround, we
                    // special case these and consider it ending a match if we have something on the
                    // stack already.
                    if ((matches[0].Item2 == SpanStartString && matches[1].Item2 == SpanEndString && spanStartStack.Peek().Item2?.Length == 0) ||
                        (matches[0].Item2 == SpanStartString && matches[1].Item2 == NamedSpanEndString && spanStartStack.Peek().Item2 != string.Empty))
                    {
                        orderedMatches.RemoveAt(0);
                    }
                }

                // Order the matches by their index
                var firstMatch = orderedMatches[0];

                var matchIndexInInput = firstMatch.Item1;
                var matchString = firstMatch.Item2;

                var matchIndexInOutput = matchIndexInInput - inputOutputOffset;
                outputBuilder.Append(input, currentIndexInInput, matchIndexInInput - currentIndexInInput);

                currentIndexInInput = matchIndexInInput + matchString.Length;
                inputOutputOffset += matchString.Length;

                switch (matchString.Substring(0, 2))
                {
                    case PositionString:
                        positions.Add(matchIndexInOutput);
                        break;

                    case SpanStartString:
                        spanStartStack.Push(Tuple.Create(matchIndexInOutput, string.Empty));
                        break;

                    case SpanEndString:
                        if (spanStartStack.Count == 0)
                        {
                            throw new ArgumentException(string.Format("Saw {0} without matching {1}", SpanEndString, SpanStartString));
                        }

                        if (spanStartStack.Peek().Item2.Length > 0)
                        {
                            throw new ArgumentException(string.Format("Saw {0} without matching {1}", NamedSpanStartString, NamedSpanEndString));
                        }

                        PopSpan(spanStartStack, spans, matchIndexInOutput);
                        break;

                    case NamedSpanStartString:
                        var name = namedSpanStartMatch.Groups[1].Value;
                        spanStartStack.Push(Tuple.Create(matchIndexInOutput, name));
                        break;

                    case NamedSpanEndString:
                        if (spanStartStack.Count == 0)
                        {
                            throw new ArgumentException(string.Format("Saw {0} without matching {1}", NamedSpanEndString, NamedSpanStartString));
                        }

                        if (spanStartStack.Peek().Item2.Length == 0)
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

        private static void PopSpan(
            Stack<Tuple<int, string>> spanStartStack,
            IDictionary<string, IList<TextSpan>> spans,
            int finalIndex)
        {
            var spanStartTuple = spanStartStack.Pop();

            var span = TextSpan.FromBounds(spanStartTuple.Item1, finalIndex);
            spans.GetOrAdd(spanStartTuple.Item2, () => new List<TextSpan>()).Add(span);
        }

        private static void AddMatch(string input, string value, int currentIndex, List<(int index, string value)> matches)
        {
            var index = input.IndexOf(value, currentIndex);
            if (index >= 0)
            {
                matches.Add((index, value));
            }
        }

        public static void GetPositionsAndSpans(string input, out string output, out IList<int> positions, out IDictionary<string, IList<TextSpan>> spans)
        {
            Parse(input, out output, out positions, out spans);
        }

        public static void GetPositionAndSpans(string input, out string output, out int? cursorPositionOpt, out IDictionary<string, IList<TextSpan>> spans)
        {
            Parse(input, out output, out var positions, out spans);
            cursorPositionOpt = positions.SingleOrDefault();
        }

        public static void GetPositionAndSpans(string input, out int? cursorPositionOpt, out IDictionary<string, IList<TextSpan>> spans)
        {
            GetPositionAndSpans(input, out var output, out cursorPositionOpt, out spans);
        }

        public static void GetPositionAndSpans(string input, out string output, out int cursorPosition, out IDictionary<string, IList<TextSpan>> spans)
        {
            GetPositionAndSpans(input, out output, out int? cursorPositionOpt, out spans);
            if (cursorPositionOpt is null)
            {
                throw new ArgumentException("The input did not include a marked cursor position", nameof(input));
            }

            cursorPosition = cursorPositionOpt.Value;
        }

        public static void GetSpans(string input, out string output, out IDictionary<string, IList<TextSpan>> spans)
        {
            GetPositionAndSpans(input, out output, out int? cursorPositionOpt, out spans);
        }

        public static void GetPositionAndSpans(string input, out string output, out int? cursorPositionOpt, out IList<TextSpan> spans)
        {
            Parse(input, out output, out var positions, out var dictionary);
            cursorPositionOpt = positions.SingleOrDefault();

            spans = dictionary.GetOrAdd(string.Empty, () => new List<TextSpan>());
        }

        public static void GetPositionAndSpans(string input, out int? cursorPositionOpt, out IList<TextSpan> spans)
        {
            GetPositionAndSpans(input, out var output, out cursorPositionOpt, out spans);
        }

        public static void GetPositionAndSpans(string input, out string output, out int cursorPosition, out IList<TextSpan> spans)
        {
            GetPositionAndSpans(input, out output, out int? pos, out spans);

            cursorPosition = pos ?? 0;
        }

        public static void GetPosition(string input, out string output, out int cursorPosition)
        {
            GetPositionAndSpans(input, out output, out cursorPosition, out IList<TextSpan> spans);
        }

        public static void GetPositionAndSpan(string input, out string output, out int cursorPosition, out TextSpan span)
        {
            GetPositionAndSpans(input, out output, out cursorPosition, out IList<TextSpan> spans);

            span = spans.Single();
        }

        public static void GetSpans(string input, out string output, out IList<TextSpan> spans)
        {
            GetPositionAndSpans(input, out output, out int? pos, out spans);
        }

        public static void GetSpan(string input, out string output, out TextSpan span)
        {
            GetSpans(input, out output, out IList<TextSpan> spans);

            span = spans.Single();
        }

        public static string CreateTestFile(string code, int cursor)
        {
            return CreateTestFile(code, new Dictionary<string, IList<TextSpan>>(), cursor);
        }

        public static string CreateTestFile(string code, IList<TextSpan> spans, int? cursor)
        {
            return CreateTestFile(code, new Dictionary<string, IList<TextSpan>> { { string.Empty, spans } }, cursor);
        }

        public static string CreateTestFile(string code, IDictionary<string, IList<TextSpan>> spans, int? cursor)
        {
            var sb = new StringBuilder();
            var anonymousSpans = spans.GetOrAdd(string.Empty, () => new List<TextSpan>());

            for (var i = 0; i <= code.Length; i++)
            {
                if (i == cursor)
                {
                    sb.Append(PositionString);
                }

                AddSpanString(sb, spans.Where(kvp => kvp.Key != string.Empty), i, start: true);
                AddSpanString(sb, spans.Where(kvp => kvp.Key?.Length == 0), i, start: true);
                AddSpanString(sb, spans.Where(kvp => kvp.Key?.Length == 0), i, start: false);
                AddSpanString(sb, spans.Where(kvp => kvp.Key != string.Empty), i, start: false);

                if (i < code.Length)
                {
                    sb.Append(code[i]);
                }
            }

            return sb.ToString();
        }

        private static void AddSpanString(
            StringBuilder sb,
            IEnumerable<KeyValuePair<string, IList<TextSpan>>> items,
            int position,
            bool start)
        {
            foreach (var kvp in items)
            {
                foreach (var span in kvp.Value)
                {
                    if (start && span.Start == position)
                    {
                        if (kvp.Key?.Length == 0)
                        {
                            sb.Append(SpanStartString);
                        }
                        else
                        {
                            sb.Append(NamedSpanStartString);
                            sb.Append(kvp.Key);
                            sb.Append(':');
                        }
                    }
                    else if (!start && span.End == position)
                    {
                        if (kvp.Key?.Length == 0)
                        {
                            sb.Append(SpanEndString);
                        }
                        else
                        {
                            sb.Append(NamedSpanEndString);
                        }
                    }
                }
            }
        }
    }
}
