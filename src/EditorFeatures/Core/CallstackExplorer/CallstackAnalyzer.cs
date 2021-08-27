// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CallstackExplorer
{
    internal static class CallstackAnalyzer
    {
        private const string StackTraceStart = "at ";
        private const string StackTraceSymbolAndFileSplit = " in ";

        internal static Task<CallstackAnalysisResults> AnalyzeAsync(string callstack, CancellationToken cancellationToken)
        {
            var parsedLines = ParseLines(callstack);

            return Task.FromResult(new CallstackAnalysisResults(
                parsedLines.ToImmutableArray()));
        }

        private static IEnumerable<ParsedLine> ParseLines(string callstack)
        {
            foreach (var line in callstack.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith(StackTraceStart))
                {
                    yield return ParseStackTraceLine(trimmedLine);
                }
                else
                {
                    if (TryParseDebugWindowStack(trimmedLine, out var result))
                    {
                        yield return result;
                    }
                }
            }
        }

        private static bool TryParseDebugWindowStack(string line, [NotNullWhen(returnValue: true)] out ParsedLine? result)
        {
            // Example line:
            // ConsoleApp4.dll!ConsoleApp4.MyClass.ThrowAtOne() Line 19	C#
            //                |--------------------------------|
            //                     Symbol data we care about
            result = null;

            var startPoint = line.IndexOf('!');
            if (startPoint == -1)
            {
                return false;
            }

            var endPoint = line.LastIndexOf(')');
            if (endPoint == -1)
            {
                return false;
            }

            // + 1 so we include the ')' character at the end
            var length = (endPoint - startPoint) + 1;

            result = new ParsedLine(line, new TextSpan(startPoint, length));
            return true;
        }

        private static ParsedLine ParseStackTraceLine(string line)
        {
            // Example line
            // at ConsoleApp4.MyClass.ThrowAtOne() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 26
            //   |--------------------------------|  |--------------------------------------------|   |--|
            //          Symbol Data                          File Information                           Line number

            Debug.Assert(line.StartsWith(StackTraceStart));

            line = line.Substring(StackTraceStart.Length);

            var symbolInformationStart = StackTraceStart.Length;

            // +1 to include the ')' at the end
            var symbolInformationEnd = (line.LastIndexOf(")") + 1);
            var symbolInformationSpan = new TextSpan(symbolInformationStart, symbolInformationEnd - symbolInformationStart);

            if (line.Contains(StackTraceSymbolAndFileSplit))
            {
                var fileInformationStart = line.IndexOf(StackTraceSymbolAndFileSplit) + StackTraceSymbolAndFileSplit.Length;
                var fileInformationSpan = new TextSpan(fileInformationStart, line.Length - fileInformationStart);

                return new FileLineResult(line, symbolInformationSpan, fileInformationSpan);
            }

            return new ParsedLine(line, symbolInformationSpan);
        }
    }
}
