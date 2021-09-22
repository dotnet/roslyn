// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.StackTraceExplorer
{
    internal static class StackTraceAnalyzer

    {
        // List of parsers to use. Order is important because
        // take the result from the first parser that returns 
        // success.
        private static readonly IStackFrameParser[] Parsers = new IStackFrameParser[]
        {
            new DotnetStackFrameParser(),
            new VSDebugCallstackParser(),
            new DefaultStackParser()
        };

        internal static Task<StackTraceAnalysisResult> AnalyzeAsync(string callstack, CancellationToken _)
        {
            var parsedFrames = Parse(callstack);
            return Task.FromResult(new StackTraceAnalysisResult(parsedFrames.ToImmutableArray()));
        }

        private static IEnumerable<ParsedFrame> Parse(string callstack)
        {
            foreach (var line in SplitLines(callstack))
            {
                var trimmedLine = line.Trim();

                foreach (var parser in Parsers)
                {
                    if (parser.TryParseLine(trimmedLine, out var parsedFrame))
                    {
                        yield return parsedFrame;
                        break;
                    }
                }
            }
        }

        private static IEnumerable<string> SplitLines(string callstack)
        {
            // ActivityLog.xml line split
            if (callstack.Contains("&#x000D;&#x000A;"))
            {
                return SplitLines(callstack, "&#x000D;&#x000A;");
            }

            return callstack.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// netstandard2.0 doesn't have a split that takes a string as the split character, 
        /// so write our own I guess :/
        /// </summary>
        private static IEnumerable<string> SplitLines(string callstack, string splitString)
        {
            var lastIndex = 0;
            var index = callstack.IndexOf(splitString);

            while (index >= 0)
            {
                var length = index - lastIndex;
                var subString = callstack.Substring(lastIndex, length);
                if (!string.IsNullOrEmpty(subString))
                {
                    yield return subString;
                }

                // Skip over the characters in the string we're splitting on
                index += splitString.Length;

                lastIndex = index;
                index = callstack.IndexOf(splitString, lastIndex + 1);
            }

            // Return any leftover string after finding all instances of splitString
            if (lastIndex >= 0 && lastIndex < callstack.Length)
            {
                yield return callstack.Substring(lastIndex);
            }
        }
    }
}
