// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    internal static class StackTraceAnalyzer

    {
        /// <summary>
        /// List of parsers to use. Order is important because
        /// take the result from the first parser that returns 
        /// success.
        /// </summary>
        private static readonly ImmutableArray<IStackFrameParser> s_parsers = ImmutableArray.Create<IStackFrameParser>(
            new DotnetStackFrameParser(),
            new VSDebugCallstackParser(),
            new DefaultStackParser()
        );

        public static Task<StackTraceAnalysisResult> AnalyzeAsync(string callstack, CancellationToken cancellationToken)
        {
            var parsedFrames = Parse(callstack, cancellationToken);
            return Task.FromResult(new StackTraceAnalysisResult(parsedFrames.ToImmutableArray()));
        }

        private static IEnumerable<ParsedFrame> Parse(string callstack, CancellationToken cancellationToken)
        {
            foreach (var line in SplitLines(callstack))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var trimmedLine = line.Trim();

                foreach (var parser in s_parsers)
                {
                    if (parser.TryParseLine(trimmedLine, out var parsedFrame))
                    {
                        yield return parsedFrame;
                        break;
                    }
                }
            }
        }

        private static readonly char[] s_lineSplit = new[] { '\n' };

        private static IEnumerable<string> SplitLines(string callstack)
        {
            // if the callstack comes from ActivityLog.xml it has been
            // encoding to be passed over HTTP. This should only decode 
            // specific characters like "&gt;" and "&lt;" to their "normal"
            // equivalents ">" and "<" so we can parse correctly
            callstack = WebUtility.HtmlDecode(callstack);

            return callstack.Split(s_lineSplit, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
