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
            foreach (var line in callstack.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
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
    }
}
