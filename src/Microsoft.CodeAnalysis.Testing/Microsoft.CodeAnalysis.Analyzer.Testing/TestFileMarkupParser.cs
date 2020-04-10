// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// <para>To aid with testing, we define a special type of text file that can encode additional
    /// information in it.  This prevents a test writer from having to carry around multiple sources
    /// of information that must be reconstituted.  For example, instead of having to keep around the
    /// contents of a file <em>and</em> and the location of the cursor, the tester can just provide a
    /// string with the <c>$$</c> character in it.  This allows for easy creation of "FIT" tests where all
    /// that needs to be provided are strings that encode every bit of state necessary in the string
    /// itself.</para>
    ///
    /// <para>The current set of encoded features we support are:</para>
    ///
    /// <list type="bullet">
    ///   <item>
    ///     <term><c>$$</c></term>
    ///     <description>A position in the file. The number of times this is allowed to appear varies depending on the
    ///     specific call.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>[|</c> ... <c>|]</c></term>
    ///     <description>A span of text in the file. There can be many of these and they can be nested and/or overlap
    ///     the <c>$$</c> position.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>{|Name:</c> ... <c>|}</c></term>
    ///     <description>A span of text in the file annotated with an identifier. There can be many of these, including
    ///     ones with the same name.</description>
    ///   </item>
    /// </list>
    ///
    /// <para>Additional encoded features can be added on a case by case basis.</para>
    /// </summary>
    public static class TestFileMarkupParser
    {
        private const string PositionString = "$$";
        private const string SpanStartString = "[|";
        private const string SpanEndString = "|]";
        private const string NamedSpanStartString = "{|";
        private const string NamedSpanEndString = "|}";
        private const string NamedSpanNumberedEndString = "|#";

        private static readonly Regex s_namedSpanStartRegex = new Regex(
            @"\{\| ([^:|[\]{}]+) \:",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex s_namedSpanEndRegex = new Regex(
            @"\| (\#\d+) \}",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        private static void Parse(string input, out string output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            Parse(input, out output, out positions, out var startPositions, out var endPositions);
            if (startPositions.Length != endPositions.Length)
            {
                throw new ArgumentException($"The input contained '{startPositions.Length}' starting spans and '{endPositions.Length}' ending spans.");
            }

            var startPositionsList = startPositions.ToImmutableList().ToBuilder();
            var endPositionsList = endPositions.ToImmutableList().ToBuilder();

            var spansBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<TextSpan>.Builder>();

            // Start by matching all end positions that were provided by ID
            for (var i = 0; i < endPositionsList.Count; i++)
            {
                var (inputPosition, outputPosition, key) = endPositionsList[i];
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                if (spansBuilder.ContainsKey(key))
                {
                    throw new ArgumentException($"The input contained more than one ending tag for span '{key}'", nameof(input));
                }

                var index = startPositionsList.FindIndex(start => start.key == key);
                if (index < 0)
                {
                    throw new ArgumentException($"The input did not contain a start tag for span '{key}'", nameof(input));
                }

                spansBuilder[key] = ImmutableArray.Create(TextSpan.FromBounds(startPositionsList[index].outputPosition, outputPosition)).ToBuilder();
                endPositionsList.RemoveAt(i);
                startPositionsList.RemoveAt(index);
                i--;
            }

            // Match the remaining spans using a simple stack algorithm
            var startIndex = 0;
            while (startPositionsList.Count > 0)
            {
                Debug.Assert(startPositionsList.Count == endPositionsList.Count, "Assertion failed: startPositionsList.Count == endPositionsList.Count");
                Debug.Assert(startIndex >= 0 && startIndex < startPositionsList.Count, "Assertion failed: startIndex >= 0 && startIndex < startPositionsList.Count");

                var (startInputPosition, startOutputPosition, startKey) = startPositionsList[startIndex];
                var (endInputPosition, endOutputPosition, endKey) = endPositionsList[0];
                Debug.Assert(endKey == string.Empty, "Assertion failed: endKey == string.Empty");
                if (startInputPosition > endInputPosition)
                {
                    if (startIndex == 0)
                    {
                        throw new ArgumentException($"Mismatched end tag found at position '{endInputPosition}'", nameof(input));
                    }

                    startIndex--;
                    (startInputPosition, startOutputPosition, startKey) = startPositionsList[startIndex];
                    startPositionsList.RemoveAt(startIndex);
                    endPositionsList.RemoveAt(0);
                    if (startKey.StartsWith("#") && spansBuilder.ContainsKey(startKey))
                    {
                        throw new ArgumentException($"The input contained more than one start tag for span '{startKey}'", nameof(input));
                    }

                    var textSpanBuilder = spansBuilder.GetOrAdd(startKey, _ => ImmutableArray.CreateBuilder<TextSpan>());
                    textSpanBuilder.Add(TextSpan.FromBounds(startOutputPosition, endOutputPosition));
                    continue;
                }
                else
                {
                    if (startIndex == startPositionsList.Count - 1)
                    {
                        startPositionsList.RemoveAt(startIndex);
                        endPositionsList.RemoveAt(0);
                        if (startKey.StartsWith("#") && spansBuilder.ContainsKey(startKey))
                        {
                            throw new ArgumentException($"The input contained more than one start tag for span '{startKey}'", nameof(input));
                        }

                        var textSpanBuilder = spansBuilder.GetOrAdd(startKey, _ => ImmutableArray.CreateBuilder<TextSpan>());
                        textSpanBuilder.Add(TextSpan.FromBounds(startOutputPosition, endOutputPosition));

                        startIndex--;
                        continue;
                    }
                    else
                    {
                        startIndex++;
                        continue;
                    }
                }
            }

            spans = spansBuilder.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutable());
        }

        /// <summary>
        /// Parses the input markup to find standalone positions and the start and end positions of text spans.
        /// </summary>
        /// <param name="input">The input markup.</param>
        /// <param name="output">The output content with markup syntax removed from <paramref name="input"/>.</param>
        /// <param name="positions">A list of positions defined in markup (<c>$$</c>).</param>
        /// <param name="startPositions">A list of starting positions of spans in markup. The key of the element is a
        /// position (the location of the <c>[|</c> or <c>{|</c>). The value of the element is the <c>text</c> content
        /// of a <c>{|text:</c> starting syntax, or <see langword="null"/> if the <c>[|</c> syntax was used. This list
        /// preserves the original order of starting markup tags in the input.</param>
        /// <param name="endPositions">A list of ending positions of spans in markup. The key of the element is a
        /// position (the location of the <c>|]</c> or <c>|}</c>). The value of the element is the <c>#id</c> content of
        /// a <c>|#id}</c> ending syntax, or <see langword="null"/> if the <c>|]</c> or <c>|}</c> syntax was used. This
        /// list preserves the original order of the ending markup tags in the input.</param>
        private static void Parse(string input, out string output, out ImmutableArray<int> positions, out ImmutableArray<(int inputPosition, int outputPosition, string key)> startPositions, out ImmutableArray<(int inputPosition, int outputPosition, string key)> endPositions)
        {
            var positionsBuilder = ImmutableArray.CreateBuilder<int>();
            var startPositionsBuilder = ImmutableArray.CreateBuilder<(int inputPosition, int outputPosition, string key)>();
            var endPositionsBuilder = ImmutableArray.CreateBuilder<(int inputPosition, int outputPosition, string key)>();

            var outputBuilder = new StringBuilder();

            var currentIndexInInput = 0;
            var inputOutputOffset = 0;

            var matches = new List<(int position, string key)>(6);
            while (true)
            {
                matches.Clear();

                AddMatch(input, PositionString, currentIndexInInput, matches);
                AddMatch(input, SpanStartString, currentIndexInInput, matches);
                AddMatch(input, SpanEndString, currentIndexInInput, matches);
                AddMatch(input, NamedSpanEndString, currentIndexInInput, matches);

                var namedSpanStartMatch = s_namedSpanStartRegex.Match(input, currentIndexInInput);
                if (namedSpanStartMatch.Success)
                {
                    matches.Add((namedSpanStartMatch.Index, namedSpanStartMatch.Value));
                }

                var namedSpanEndMatch = s_namedSpanEndRegex.Match(input, currentIndexInInput);
                if (namedSpanEndMatch.Success)
                {
                    matches.Add((namedSpanEndMatch.Index, namedSpanEndMatch.Value));
                }

                if (matches.Count == 0)
                {
                    // No more markup to process.
                    break;
                }

                var orderedMatches = matches.OrderBy(t => t.position).ToList();
                if (orderedMatches.Count >= 2 &&
                    endPositionsBuilder.Count < startPositionsBuilder.Count &&
                    matches[0].position == matches[1].position - 1)
                {
                    // We have a slight ambiguity with cases like these:
                    //
                    // [|]    [|}
                    //
                    // Is it starting a new match, or ending an existing match.  As a workaround, we
                    // special case these and consider it ending a match if we have something on the
                    // stack already.
                    var (_, _, lastUnmatchedStartKey) = GetLastUnmatchedSpanStart(startPositionsBuilder, endPositionsBuilder);
                    if ((matches[0].key == SpanStartString && matches[1].key == SpanEndString && lastUnmatchedStartKey.Length == 0) ||
                        (matches[0].key == SpanStartString && matches[1].key == NamedSpanEndString && lastUnmatchedStartKey != string.Empty))
                    {
                        orderedMatches.RemoveAt(0);
                    }
                }

                // Order the matches by their index
                var firstMatch = orderedMatches[0];

                var matchIndexInInput = firstMatch.position;
                var matchString = firstMatch.key;

                var matchIndexInOutput = matchIndexInInput - inputOutputOffset;
                outputBuilder.Append(input, currentIndexInInput, matchIndexInInput - currentIndexInInput);

                currentIndexInInput = matchIndexInInput + matchString.Length;
                inputOutputOffset += matchString.Length;

                switch (matchString.Substring(0, 2))
                {
                    case PositionString:
                        positionsBuilder.Add(matchIndexInOutput);
                        break;

                    case SpanStartString:
                        startPositionsBuilder.Add((matchIndexInInput, matchIndexInOutput, string.Empty));
                        break;

                    case SpanEndString:
                        endPositionsBuilder.Add((matchIndexInInput, matchIndexInOutput, string.Empty));
                        break;

                    case NamedSpanStartString:
                        var name = namedSpanStartMatch.Groups[1].Value;
                        startPositionsBuilder.Add((matchIndexInInput, matchIndexInOutput, name));
                        break;

                    case NamedSpanEndString:
                        endPositionsBuilder.Add((matchIndexInInput, matchIndexInOutput, string.Empty));
                        break;

                    case NamedSpanNumberedEndString:
                        name = namedSpanEndMatch.Groups[1].Value;
                        endPositionsBuilder.Add((matchIndexInInput, matchIndexInOutput, name));
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            // Append the remainder of the string.
            outputBuilder.Append(input.Substring(currentIndexInInput));
            output = outputBuilder.ToString();
            positions = positionsBuilder.ToImmutable();
            startPositions = startPositionsBuilder.ToImmutable();
            endPositions = endPositionsBuilder.ToImmutable();
            return;

            // Local functions
            static (int inputPosition, int outputPosition, string key) GetLastUnmatchedSpanStart(ImmutableArray<(int inputPosition, int outputPosition, string key)>.Builder startPositionsBuilder, ImmutableArray<(int inputPosition, int outputPosition, string key)>.Builder endPositionsBuilder)
            {
                // For disambiguating [|] and [|}, assume that the start and end tags are behaving like a stack
                Debug.Assert(startPositionsBuilder.Count > endPositionsBuilder.Count, "Assertion failed: startPositionsBuilder.Count > endPositionsBuilder.Count");

                var stackDepth = 0;
                var startPositionIndex = startPositionsBuilder.Count - 1;
                var endPositionIndex = endPositionsBuilder.Count - 1;
                while (true)
                {
                    if (endPositionIndex < 0)
                    {
                        // The are no more end tags. Pop the ones remaining on the stack and return the last remaining
                        // start tag.
                        return startPositionsBuilder[startPositionIndex - stackDepth];
                    }

                    if (startPositionsBuilder[startPositionIndex].inputPosition > endPositionsBuilder[endPositionIndex].inputPosition)
                    {
                        if (stackDepth == 0)
                        {
                            // Reached an unmatched start tag.
                            return startPositionsBuilder[startPositionIndex];
                        }

                        // "pop" the start tag off the stack
                        stackDepth--;
                        startPositionIndex--;
                    }
                    else
                    {
                        // "push" the end tag onto the stack
                        stackDepth++;
                        endPositionIndex--;
                    }
                }
            }
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

        public static void GetPositionsAndSpans(string input, out string output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            Parse(input, out output, out positions, out spans);
        }

        public static void GetPositionAndSpans(string input, out string output, out int? cursorPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            Parse(input, out output, out var positions, out spans);
            cursorPosition = positions.SingleOrNull();
        }

        public static void GetPositionAndSpans(string input, out int? cursorPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            GetPositionAndSpans(input, out _, out cursorPosition, out spans);
        }

        public static void GetPositionAndSpans(string input, out string output, out int cursorPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            GetPositionAndSpans(input, out output, out int? cursorPositionOpt, out spans);
            cursorPosition = cursorPositionOpt ?? throw new ArgumentException("The input did not include a marked cursor position", nameof(input));
        }

        public static void GetSpans(string input, out string output, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            GetPositionAndSpans(input, out output, out int? _, out spans);
        }

        public static void GetPositionAndSpans(string input, out string output, out int? cursorPosition, out ImmutableArray<TextSpan> spans)
        {
            Parse(input, out output, out var positions, out var dictionary);
            cursorPosition = positions.SingleOrNull();

            spans = dictionary.GetValueOrDefault(string.Empty, ImmutableArray<TextSpan>.Empty);
        }

        public static void GetPositionAndSpans(string input, out int? cursorPosition, out ImmutableArray<TextSpan> spans)
        {
            GetPositionAndSpans(input, out _, out cursorPosition, out spans);
        }

        public static void GetPositionAndSpans(string input, out string output, out int cursorPosition, out ImmutableArray<TextSpan> spans)
        {
            GetPositionAndSpans(input, out output, out int? cursorPositionOpt, out spans);
            cursorPosition = cursorPositionOpt ?? throw new ArgumentException("The input did not include a marked cursor position", nameof(input));
        }

        /// <summary>
        /// Process markup containing exactly one position.
        /// </summary>
        /// <param name="input">The input markup.</param>
        /// <param name="output">The output, with markup syntax removed.</param>
        /// <param name="cursorPosition">The location of the <c>$$</c> position in <paramref name="input"/>.</param>
        /// <exception cref="ArgumentException">If <paramref name="input"/> does not contain exactly one position,
        /// indicated by <c>$$</c>.</exception>
        public static void GetPosition(string input, out string output, out int cursorPosition)
        {
            GetPositionAndSpans(input, out output, out cursorPosition, out ImmutableArray<TextSpan> _);
        }

        public static void GetPositionAndSpan(string input, out string output, out int cursorPosition, out TextSpan span)
        {
            GetPositionAndSpans(input, out output, out cursorPosition, out ImmutableArray<TextSpan> spans);

            span = spans.Single();
        }

        public static void GetSpans(string input, out string output, out ImmutableArray<TextSpan> spans)
        {
            GetPositionAndSpans(input, out output, out int? _, out spans);
        }

        public static void GetSpan(string input, out string output, out TextSpan span)
        {
            GetSpans(input, out output, out ImmutableArray<TextSpan> spans);

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
