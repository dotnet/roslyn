// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

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

            // +1 here because we always want to skip the '!' character
            var startPoint = line.IndexOf('!') + 1;

            if (startPoint == 0 || startPoint == line.Length)
            {
                return false;
            }

            var success = StackFrameParserHelpers.TryParseMethodSignature(line.AsSpan().Slice(startPoint), out var classSpan, out var methodSpan, out var argsSpan);

            if (!success)
            {
                return false;
            }

            // The spans need to be fixed up by the start point since we didn't
            // pass everyting from '!' and before to the parser
            classSpan = new TextSpan(classSpan.Start + startPoint, classSpan.Length);
            methodSpan = new TextSpan(methodSpan.Start + startPoint, methodSpan.Length);
            argsSpan = new TextSpan(argsSpan.Start + startPoint, argsSpan.Length);

            parsedFrame = new ParsedStackFrame(line, classSpan, methodSpan, argsSpan);
            return true;
        }
    }
}
