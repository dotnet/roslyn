// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CallstackExplorer
{
    internal static class CallstackAnalyzer
    {
        private const string StackTraceStart = "at ";
        private const string StackTraceSymbolAndFileSplit = " in ";

        internal static Task<CallstackAnalysisResults> AnalyzeAsync(string callstack, CancellationToken _)
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

                if (TryParseDebugWindowStack(trimmedLine, out var parsedLine))
                {
                    yield return parsedLine;
                    continue;
                }

                if (TryParseStackTraceLine(trimmedLine, out parsedLine))
                {
                    yield return parsedLine;
                    continue;
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

            var success = TryParseMethodSignature(line, skipCharacters: startPoint, out var classSpan, out var methodSpan, out var argsSpan);

            if (!success)
            {
                return false;
            }

            result = new ParsedLine(line, classSpan, methodSpan, argsSpan);
            return true;
        }

        private static bool TryParseStackTraceLine(string line, [NotNullWhen(returnValue: true)] out ParsedLine? parsedLine)
        {
            parsedLine = null;
            var success = TryParseMethodSignature(line, skipCharacters: 0, out var classSpan, out var methodSpan, out var argsSpan);

            if (!success)
            {
                return false;
            }

            // The line has " in <filename>:line <line number>"
            if (line.Contains(StackTraceSymbolAndFileSplit))
            {
                var fileInformationStart = line.IndexOf(StackTraceSymbolAndFileSplit) + StackTraceSymbolAndFileSplit.Length;
                var fileInformationSpan = new TextSpan(fileInformationStart, line.Length - fileInformationStart);

                parsedLine = new FileLineResult(line, classSpan, methodSpan, argsSpan, fileInformationSpan);
                return true;
            }

            parsedLine = new ParsedLine(line, classSpan, methodSpan, argsSpan);
            return true;
        }

        private static TextSpan MakeSpanFromEndpoints(int start, int end)
        {
            Contract.ThrowIfTrue(start > end);
            Contract.ThrowIfTrue(start < 0);

            var length = end - start;
            return new TextSpan(start, length);
        }

        /// <summary>
        /// Makes sure that the string at least somewhat resembles the correct form.
        /// Does not check validity on class or method identifiers
        /// Example line:
        /// at ConsoleApp4.MyClass.ThrowAtOne(p1, p2,) 
        ///   |-------------------||--------||-------| 
        ///           Class          Method    Args   
        /// </summary>
        /// <remarks>
        /// See https://docs.microsoft.com/en-us/dotnet/api/system.environment.stacktrace for more information
        /// on expected stacktrace form
        /// </remarks>

        private static bool TryParseMethodSignature(string line, int skipCharacters, out TextSpan classSpan, out TextSpan methodSpan, out TextSpan argsSpan)
        {
            Contract.ThrowIfTrue(skipCharacters < 0);

            classSpan = default;
            methodSpan = default;
            argsSpan = default;

            if (skipCharacters > 0)
            {
                line = line[skipCharacters..];
            }

            var regex = new Regex(@"(?<class>([a-zA-Z0-9_]+\.)+)(?<method>[a-zA-Z0-9_]+)\((?<args>.*)\).*");
            if (!regex.IsMatch(line))
            {
                return false;
            }

            var match = regex.Match(line);
            if (!match.Success)
            {
                return false;
            }

            var classGroup = match.Groups["class"];
            if (!classGroup.Success)
            {
                return false;
            }

            var methodGroup = match.Groups["method"];
            if (!methodGroup.Success)
            {
                return false;
            }

            var argsGroup = match.Groups["args"];

            classSpan = new TextSpan(skipCharacters + classGroup.Index, classGroup.Length);
            methodSpan = new TextSpan(skipCharacters + methodGroup.Index, methodGroup.Length);
            argsSpan = argsGroup.Success
                ? new TextSpan(skipCharacters + argsGroup.Index, argsGroup.Length)
                : default;

            return true;
        }
    }
}
