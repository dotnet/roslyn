// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    internal sealed class VSDebugCallstackParser : IStackFrameParser
    {
        public bool TryParseLine(string line, [NotNullWhen(true)] out ParsedFrame? parsedFrame)
        {
            // Example line:
            // ConsoleApp4.dll!ConsoleApp4.MyClass.ThrowAtOne() Line 19	C#
            //                |--------------------------------|
            //                     Symbol data we care about
            parsedFrame = null;

            var startPoint = line.IndexOf('!');
            if (startPoint == -1)
            {
                return false;
            }

            var success = StackFrameParserHelpers.TryParseMethodSignature(line, start: startPoint, end: line.Length, out var classSpan, out var methodSpan, out var argsSpan);

            if (!success)
            {
                return false;
            }

            parsedFrame = new ParsedStackFrame(line, classSpan, methodSpan, argsSpan);
            return true;
        }
    }
}
