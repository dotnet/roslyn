// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    internal static class CommandArgumentsParser
    {
        internal static void SkipWhiteSpace(string args, ref int i)
        {
            while (!IsEnd(args, i) && char.IsWhiteSpace(args[i]))
            {
                i++;
            }
        }

        internal static bool IsSingleLineCommentStart(string args, int i)
        {
            return i + 1 < args.Length && args[i] == '/' && args[i + 1] == '/';
        }

        internal static bool IsEnd(string args, int i)
        {
            return i == args.Length || args[i] == '\n' || args[i] == '\r';
        }

        internal static void SkipSingleLineComment(string args, ref int i)
        {
            if (IsSingleLineCommentStart(args, i))
            {
                i += 2;
                while (!IsEnd(args, i))
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// Parses an optional path argument. Path is a double-quoted string with no escapes.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <param name="i">Current position in <paramref name="args"/>.</param>
        /// <param name="path">The path (might be incomplete if closing quote is missing) or null if the argument is missing.</param>
        /// <returns>True iff parsing succeeds.</returns>
        internal static bool ParsePath(string args, ref int i, out string path)
        {
            int stringStart, stringEnd;
            return ParsePath(args, ref i, out path, out stringStart, out stringEnd);
        }

        internal static bool ParsePath(string args, ref int i, out string path, out int stringStart, out int stringEnd)
        {
            SkipWhiteSpace(args, ref i);

            if (IsEnd(args, i) || args[i] != '"')
            {
                path = null;
                stringStart = stringEnd = -1;
                return true;
            }

            stringStart = i;
            i++;
            int start = i;
            var result = false;

            while (!IsEnd(args, i))
            {
                if (args[i] == '"')
                {
                    result = true;
                    break;
                }

                i++;
            }

            // missing closing quote
            path = args.Substring(start, i - start);

            // skip closing quote:
            if (result)
            {
                i++;
            }

            stringEnd = i;
            return result;
        }

        /// <summary>
        /// Parses an optional trailing single-line C# comment, whitespace and line breaks.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <param name="i">Current position in <paramref name="args"/>.</param>
        /// <returns>True iff parsing succeeds.</returns>
        internal static bool ParseTrailingTrivia(string args, ref int i)
        {
            int commentStart, commentEnd;
            return ParseTrailingTrivia(args, ref i, out commentStart, out commentEnd);
        }

        internal static bool ParseTrailingTrivia(string args, ref int i, out int commentStart, out int commentEnd)
        {
            SkipWhiteSpace(args, ref i);

            commentStart = i;
            SkipSingleLineComment(args, ref i);
            commentEnd = i;

            // skip trailing whitespace and new lines:
            while (i < args.Length && char.IsWhiteSpace(args[i]))
            {
                i++;
            }

            return i == args.Length;
        }

        /// <summary>
        /// Parses an optional double-quoted string argument. The string may contain backslash-escaped quotes and backslashes.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <param name="i">Current position in <paramref name="args"/>.</param>
        /// <param name="result">The string.</param>
        /// <returns>True iff parsing succeeds.</returns>
        internal static bool ParseQuotedString(string args, ref int i, out string result)
        {
            int start, end;
            return ParseQuotedString(args, ref i, out result, out start, out end);
        }

        internal static bool ParseQuotedString(string args, ref int i, out string result, out int start, out int end)
        {
            result = null;
            SkipWhiteSpace(args, ref i);

            if (IsEnd(args, i) || args[i] != '"')
            {
                start = end = -1;
                return true;
            }

            start = i;
            i++;

            var sb = new StringBuilder();
            while (!IsEnd(args, i))
            {
                if (args[i] == '"')
                {
                    result = sb.ToString();
                    i++;
                    end = i;
                    return true;
                }

                if (args[i] == '\\' && i + 1 < args.Length)
                {
                    i++;
                    if (args[i] == '"' || args[i] == '\\')
                    {
                        sb.Append(args[i]);
                    }
                    else
                    {
                        end = i;
                        return false;
                    }
                }
                else
                {
                    sb.Append(args[i]);
                }

                i++;
            }

            // missing closing quote
            end = i;
            return false;
        }
    }
}
