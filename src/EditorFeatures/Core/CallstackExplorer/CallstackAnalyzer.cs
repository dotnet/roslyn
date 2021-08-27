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

namespace Microsoft.CodeAnalysis.Editor.CallstackExplorer
{
    internal static class CallstackAnalyzer
    {
        internal static Task<CallstackAnalysisResults> AnalyzeAsync(Solution currentSolution, string callstack, CancellationToken cancellationToken)
        {
            var parsedLines = ParseLines(callstack);

            return Task.FromResult(new CallstackAnalysisResults(
                currentSolution,
                parsedLines.ToImmutableArray()));
        }

        private static IEnumerable<ParsedLine> ParseLines(string callstack)
        {
            foreach (var line in callstack.Split('\n'))
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.TrimStart().StartsWith("at "))
                {
                    yield return ParseStackTraceLine(line);
                }
                else
                {
                    if (TryParseDebugWindowStack(line, out var result))
                    {
                        yield return result;
                    }
                }
            }
        }

        private static bool TryParseDebugWindowStack(string line, [NotNullWhen(returnValue: true)] out ParsedLine? result)
        {
            // >	    ConsoleApp4.dll!ConsoleApp4.MyClass.ThrowAtOne() Line 19	C#
            //          ConsoleApp4.dll!ConsoleApp4.MyClass.ThrowReferenceOne() Line 24 C#
            //          ConsoleApp4.dll!ConsoleApp4.MyClass.ToString() Line 29  C#
            //          ConsoleApp4.dll!ConsoleApp4.MyOtherClass.ThrowForNewMyClass() Line 39   C#
            //          ConsoleApp4.dll!ConsoleApp4.Program.Main(string[] args) Line 10 C#
            result = null;

            var splitIndex = line.IndexOf('!');
            if (splitIndex == -1)
            {
                return false;
            }

            var symbolData = line.Substring(splitIndex + 1);

            // Extra information may be appended, for example: 
            // ConsoleApp4.dll!ConsoleApp4.MyClass.ThrowAtOne() Line 19	C#
            // For now we're going to throw away the line number 
            // and anything following the last ) 
            var trimPoint = symbolData.LastIndexOf(')');
            if (trimPoint == -1)
            {
                return false;
            }

            symbolData = symbolData.Substring(0, trimPoint + 1);

            result = new DebugWindowResult(symbolData);
            return true;
        }

        private static ParsedLine ParseStackTraceLine(string line)
        {
            // at ConsoleApp4.MyClass.ThrowAtOne() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 26
            // at ConsoleApp4.MyClass.ThrowReferenceOne() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 31
            // at ConsoleApp4.MyClass.ToString() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 36
            // at ConsoleApp4.MyOtherClass.ThrowForNewMyClass() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 46
            // at ConsoleApp4.Program.Main(String[] args) in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 12


            // at ConsoleApp4.MyClass.ThrowAtOne() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 26
            line = line.Trim();
            Debug.Assert(line.StartsWith("at"));

            line = line.Substring("at".Length);

            // ConsoleApp4.MyClass.ThrowAtOne() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 26
            if (line.Contains(" in "))
            {
                var inIndex = line.IndexOf(" in ");
                var methodSignature = line.Substring(0, inIndex);
                var fileInformation = line.Substring(inIndex + " in ".Length);

                // ConsoleApp4.MyClass.ThrowAtOne(), C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 26
                return new FileLineResult(methodSignature, fileInformation);
            }

            return new StackTraceResult(line.Substring(0, line.LastIndexOf(")")));
        }
    }
}
