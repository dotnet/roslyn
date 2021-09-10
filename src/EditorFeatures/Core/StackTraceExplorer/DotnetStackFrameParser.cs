// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.StackTraceExplorer
{
    internal sealed class DotnetStackFrameParser : IStackFrameParser
    {
        private const string StackTraceStart = "at ";
        private const string StackTraceSymbolAndFileSplit = " in ";

        /// <summary>
        /// Tries to parse a StackFrame following convention from Environment.StackTrace
        /// https://docs.microsoft.com/en-us/dotnet/api/system.environment.stacktrace has
        /// details on output format and expected strings
        /// </summary>
        public bool TryParseLine(string line, [NotNullWhen(true)] out ParsedFrame? parsedFrame)
        {
            parsedFrame = null;
            var success = StackFrameParserHelpers.TryParseMethodSignature(line, skipCharacters: 0, out var classSpan, out var methodSpan, out var argsSpan);

            if (!success)
            {
                return false;
            }

            // The line has " in <filename>:line <line number>"
            if (line.Contains(StackTraceSymbolAndFileSplit))
            {
                var fileInformationStart = line.IndexOf(StackTraceSymbolAndFileSplit) + StackTraceSymbolAndFileSplit.Length;
                var fileInformationSpan = new TextSpan(fileInformationStart, line.Length - fileInformationStart);

                parsedFrame = new ParsedFrameWithFile(line, classSpan, methodSpan, argsSpan, fileInformationSpan);
                return true;
            }

            parsedFrame = new ParsedStackFrame(line, classSpan, methodSpan, argsSpan);
            return true;
        }
    }
}
