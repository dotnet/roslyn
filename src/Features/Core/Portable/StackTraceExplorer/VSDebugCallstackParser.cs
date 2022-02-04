// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    internal sealed class VSDebugCallstackParser : IStackFrameParser
    {
        public bool TryParseLine(VirtualCharSequence virtualChars, [NotNullWhen(true)] out ParsedFrame? parsedFrame)
        {
            // Example line:
            // ConsoleApp4.dll!ConsoleApp4.MyClass.ThrowAtOne() Line 19	C#
            //                |--------------------------------|
            //                     Symbol data we care about
            parsedFrame = null;

            var startPoint = 0;
            foreach (var virtualChar in virtualChars)
            {
                if (virtualChar.Value == '!')
                {
                    startPoint = virtualChar.Span.End;
                    break;
                }
            }

            if (startPoint == 0)
            {
                return false;
            }

            var textToParse = virtualChars.GetSubSequence(TextSpan.FromBounds(startPoint, virtualChars.Last().Span.End));
            var tree = StackFrameParser.TryParse(textToParse);

            if (tree is null)
            {
                return false;
            }

            parsedFrame = new ParsedStackFrame(tree);
            return true;
        }
    }
}
