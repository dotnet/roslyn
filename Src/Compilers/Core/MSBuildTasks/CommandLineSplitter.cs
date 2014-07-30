// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <remarks>
    /// Rules for command line parsing, according to MSDN:
    /// 
    /// Arguments are delimited by white space, which is either a space or a tab.
    ///  
    /// A string surrounded by double quotation marks ("string") is interpreted 
    /// as a single argument, regardless of white space contained within. 
    /// A quoted string can be embedded in an argument.
    ///  
    /// A double quotation mark preceded by a backslash (\") is interpreted as a 
    /// literal double quotation mark character (").
    ///  
    /// Backslashes are interpreted literally, unless they immediately precede a 
    /// double quotation mark.
    ///  
    /// If an even number of backslashes is followed by a double quotation mark, 
    /// one backslash is placed in the argv array for every pair of backslashes, 
    /// and the double quotation mark is interpreted as a string delimiter.
    ///  
    /// If an odd number of backslashes is followed by a double quotation mark, 
    /// one backslash is placed in the argv array for every pair of backslashes, 
    /// and the double quotation mark is "escaped" by the remaining backslash, 
    /// causing a literal double quotation mark (") to be placed in argv.
    /// </remarks>
    internal static class CommandLineSplitter
    {
        public static bool IsDelimiter(char c)
        {
            return c == ' ' || c == '\t';
        }

        public static bool IsQuote(char c)
        {
            // Only double quotes are respected, according to MSDN.
            return c == '\"';
        }

        private const char Backslash = '\\';

        // Split a command line by the same rules as Main would get the commands.
        public static string[] SplitCommandLine(string commandLine)
        {
            bool inQuotes = false;
            int backslashCount = 0;

            return commandLine.Split(c =>
            {
                if (c == Backslash)
                {
                    backslashCount += 1;
                }
                else if (IsQuote(c))
                {
                    if ((backslashCount & 1) != 1)
                    {
                        inQuotes = !inQuotes;
                    }
                    backslashCount = 0;
                }
                else
                {
                    backslashCount = 0;
                }

                return !inQuotes && IsDelimiter(c);
            })
            .Select(arg => arg.Trim().CondenseDoubledBackslashes().TrimMatchingQuotes())
            .Where(arg => !string.IsNullOrEmpty(arg))
            .ToArray();
        }

        // Split a string, based on whether "splitHere" returned true on each character.
        private static IEnumerable<string> Split(this string str,
                                                 Func<char, bool> splitHere)
        {
            int nextPiece = 0;

            for (int c = 0; c < str.Length; c++)
            {
                if (splitHere(str[c]))
                {
                    yield return str.Substring(nextPiece, c - nextPiece);
                    nextPiece = c + 1;
                }
            }

            yield return str.Substring(nextPiece);
        }

        // Trim leading and trailing quotes from a string, if they are there. Only trims
        // one pair.
        private static string TrimMatchingQuotes(this string input)
        {
            if ((input.Length >= 2) &&
                (IsQuote(input[0])) &&
                (IsQuote(input[input.Length - 1])))
            {
                return input.Substring(1, input.Length - 2);
            }
            else
            {
                return input;
            }
        }

        // Condense double backslashes that precede a quotation mark to single backslashes.
        private static string CondenseDoubledBackslashes(this string input)
        {
            // Simple case -- no backslashes.
            if (!input.Contains(Backslash))
                return input;

            StringBuilder builder = new StringBuilder();
            int doubleQuoteCount = 0;

            foreach (char c in input)
            {
                if (c == Backslash)
                {
                    ++doubleQuoteCount;
                }
                else
                {
                    // Add right amount of pending backslashes.
                    if (IsQuote(c))
                    {
                        AddBackslashes(builder, doubleQuoteCount / 2);
                    }
                    else
                    {
                        AddBackslashes(builder, doubleQuoteCount);
                    }

                    builder.Append(c);
                    doubleQuoteCount = 0;
                }
            }

            AddBackslashes(builder, doubleQuoteCount);
            return builder.ToString();
        }

        private static void AddBackslashes(StringBuilder builder, int count)
        {
            for (int i = 0; i < count; ++i)
                builder.Append(Backslash);
        }
    }
}
