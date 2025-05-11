// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.StackTraceExplorer;

internal static class StackTraceAnalyzer

{
    /// <summary>
    /// List of parsers to use. Order is important because
    /// take the result from the first parser that returns 
    /// success.
    /// </summary>
    private static readonly ImmutableArray<IStackFrameParser> s_parsers = [new DotnetStackFrameParser(), new VSDebugCallstackParser(), new DefaultStackParser()];

    public static Task<StackTraceAnalysisResult> AnalyzeAsync(string callstack, CancellationToken cancellationToken)
    {
        var result = new StackTraceAnalysisResult(callstack, Parse(callstack, cancellationToken));
        return Task.FromResult(result);
    }

    private static ImmutableArray<ParsedFrame> Parse(string callstack, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<ParsedFrame>.GetInstance(out var builder);

        // if the callstack comes from ActivityLog.xml it has been
        // encoding to be passed over HTTP. This should only decode 
        // specific characters like "&gt;" and "&lt;" to their "normal"
        // equivalents ">" and "<" so we can parse correctly
        callstack = WebUtility.HtmlDecode(callstack);

        var sequence = VirtualCharSequence.Create(0, callstack);

        foreach (var line in SplitLines(sequence))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // For now do the work to removing leading and trailing whitespace. 
            // This keeps behavior we've had, but may not actually be the desired behavior in the long run.
            // Specifically if we ever want to add a copy feature to copy back contents from a frame
            var trimmedLine = Trim(line);

            if (trimmedLine.IsEmpty)
            {
                continue;
            }

            foreach (var parser in s_parsers)
            {
                if (parser.TryParseLine(trimmedLine, out var parsedFrame))
                {
                    builder.Add(parsedFrame);
                    break;
                }
            }
        }

        return builder.ToImmutableAndClear();
    }

    private static IEnumerable<VirtualCharSequence> SplitLines(VirtualCharSequence callstack)
    {
        var position = 0;

        for (var i = 0; i < callstack.Length; i++)
        {
            if (callstack[i].Value == '\n')
            {
                yield return callstack.GetSubSequence(TextSpan.FromBounds(position, i));

                // +1 to skip over the \n character
                position = i + 1;
            }
        }

        if (position < callstack.Length)
        {
            yield return callstack.GetSubSequence(TextSpan.FromBounds(position, callstack.Length));
        }
    }

    private static VirtualCharSequence Trim(VirtualCharSequence virtualChars)
    {
        if (virtualChars.Length == 0)
        {
            return virtualChars;
        }

        var start = 0;
        var end = virtualChars.Length - 1;

        while (virtualChars[start].IsWhiteSpace && start < end)
        {
            start++;
        }

        while (virtualChars[end].IsWhiteSpace && end > start)
        {
            end--;
        }

        return virtualChars.GetSubSequence(TextSpan.FromBounds(start, end + 1));
    }
}
