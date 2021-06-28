// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.Model;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Extensions on the <see cref="IVerifier"/> interface.
    /// </summary>
    public static class IVerifierExtensions
    {
        private static readonly InlineDiffBuilder s_diffWithoutLineEndings = new InlineDiffBuilder(new Differ());
        private static readonly InlineDiffBuilder s_diffWithLineEndings = new InlineDiffBuilder(new DifferWithLineEndings());

        /// <summary>
        /// Asserts that two strings are equal, and prints a diff between the two if they are not.
        /// </summary>
        /// <param name="verifier">The verifier instance.</param>
        /// <param name="expected">The expected string. This is presented as the "baseline/before" side in the diff.</param>
        /// <param name="actual">The actual string. This is presented as the changed or "after" side in the diff.</param>
        /// <param name="message">The message to precede the diff, if the values are not equal.</param>
        public static void EqualOrDiff(this IVerifier verifier, string expected, string actual, string? message = null)
        {
            Requires.NotNull(verifier, nameof(verifier));

            if (expected != actual)
            {
                var diff = s_diffWithoutLineEndings.BuildDiffModel(expected, actual, ignoreWhitespace: false);
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine(
                    string.IsNullOrEmpty(message)
                        ? "Actual and expected values differ. Expected shown in baseline of diff:"
                        : message);

                if (!diff.Lines.Any(line => line.Type == ChangeType.Inserted || line.Type == ChangeType.Deleted))
                {
                    // We have a failure only caused by line ending differences; recalculate with line endings visible
                    diff = s_diffWithLineEndings.BuildDiffModel(expected, actual, ignoreWhitespace: false);
                }

                foreach (var line in diff.Lines)
                {
                    switch (line.Type)
                    {
                        case ChangeType.Inserted:
                            messageBuilder.Append("+");
                            break;
                        case ChangeType.Deleted:
                            messageBuilder.Append("-");
                            break;
                        default:
                            messageBuilder.Append(" ");
                            break;
                    }

                    messageBuilder.AppendLine(line.Text);
                }

                verifier.Fail(messageBuilder.ToString());
            }
        }

        private class DifferWithLineEndings : IDiffer
        {
            private const string CarriageReturnText = "<CR>";
            private const string LineFeedText = "<LF>";

            private static readonly char[] s_endOfLineCharacters = { '\r', '\n' };
            private static readonly Differ s_differ = new Differ();

            public DiffResult CreateCharacterDiffs(string oldText, string newText, bool ignoreWhitespace)
                => s_differ.CreateCharacterDiffs(oldText, newText, ignoreWhitespace);

            public DiffResult CreateCharacterDiffs(string oldText, string newText, bool ignoreWhitespace, bool ignoreCase)
                => s_differ.CreateCharacterDiffs(oldText, newText, ignoreWhitespace, ignoreCase);

            public DiffResult CreateCustomDiffs(string oldText, string newText, bool ignoreWhiteSpace, Func<string, string[]> chunker)
                => s_differ.CreateCustomDiffs(oldText, newText, ignoreWhiteSpace, chunker);

            public DiffResult CreateCustomDiffs(string oldText, string newText, bool ignoreWhiteSpace, bool ignoreCase, Func<string, string[]> chunker)
                => s_differ.CreateCustomDiffs(oldText, newText, ignoreWhiteSpace, ignoreCase, chunker);

            public DiffResult CreateLineDiffs(string oldText, string newText, bool ignoreWhitespace)
                => CreateLineDiffs(oldText, newText, ignoreWhitespace, ignoreCase: false);

            public DiffResult CreateLineDiffs(string oldText, string newText, bool ignoreWhitespace, bool ignoreCase)
            {
                Func<string, string[]> chunker = s =>
                {
                    var lines = new List<string>();

                    var nextChar = 0;
                    while (nextChar < s.Length)
                    {
                        var nextEol = s.IndexOfAny(s_endOfLineCharacters, nextChar);
                        if (nextEol == -1)
                        {
                            lines.Add(s.Substring(nextChar));
                            break;
                        }

                        var currentLine = s.Substring(nextChar, nextEol - nextChar);

                        switch (s[nextEol])
                        {
                            case '\r':
                                currentLine += CarriageReturnText;
                                if (nextEol < s.Length - 1 && s[nextEol + 1] == '\n')
                                {
                                    currentLine += LineFeedText;
                                    nextEol++;
                                }

                                break;

                            case '\n':
                                currentLine += LineFeedText;
                                break;
                        }

                        lines.Add(currentLine);
                        nextChar = nextEol + 1;
                    }

                    return lines.ToArray();
                };

                return CreateCustomDiffs(oldText, newText, ignoreWhitespace, ignoreCase, chunker);
            }

            public DiffResult CreateWordDiffs(string oldText, string newText, bool ignoreWhitespace, char[] separators)
                => s_differ.CreateWordDiffs(oldText, newText, ignoreWhitespace, separators);

            public DiffResult CreateWordDiffs(string oldText, string newText, bool ignoreWhitespace, bool ignoreCase, char[] separators)
                => s_differ.CreateWordDiffs(oldText, newText, ignoreWhitespace, ignoreCase, separators);
        }
    }
}
