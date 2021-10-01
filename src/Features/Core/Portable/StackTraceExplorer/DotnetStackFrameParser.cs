// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    internal sealed class DotnetStackFrameParser : IStackFrameParser
    {
        private const string StackTraceAtStart = "at ";
        private const string StackTraceSymbolAndFileSplit = " in ";

        /// <summary>
        /// Tries to parse a StackFrame following convention from Environment.StackTrace
        /// https://docs.microsoft.com/en-us/dotnet/api/system.environment.stacktrace has
        /// details on output format and expected strings
        /// 
        /// Example:
        /// at ConsoleApp4.MyClass.M() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 26
        /// </summary>
        public bool TryParseLine(string line, [NotNullWhen(true)] out ParsedFrame? parsedFrame)
        {
            parsedFrame = null;

            if (!line.Trim().StartsWith(StackTraceAtStart))
            {
                return false;
            }

            var success = StackFrameParserHelpers.TryParseMethodSignature(line.AsSpan(), out var classSpan, out var methodSpan, out var argsSpan);
            if (!success)
            {
                return false;
            }

            var splitIndex = line.IndexOf(StackTraceSymbolAndFileSplit);

            // The line has " in <filename>:line <line number>"
            if (splitIndex > 0)
            {
                var fileInformationStart = splitIndex + StackTraceSymbolAndFileSplit.Length;
                var fileInformationSpan = new TextSpan(fileInformationStart, line.Length - fileInformationStart);

                parsedFrame = new ParsedStackFrame(line, classSpan, methodSpan, argsSpan, fileInformationSpan);
                return true;
            }

            parsedFrame = new ParsedStackFrame(line, classSpan, methodSpan, argsSpan);
            return true;
        }
    }
}
