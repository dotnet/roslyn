// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.StackTraceExplorer;

internal sealed class DotnetStackFrameParser : IStackFrameParser
{
    /// <summary>
    /// Uses <see cref="StackFrameParser"/> to parse a line if possible
    /// </summary>
    public bool TryParseLine(VirtualCharSequence line, [NotNullWhen(true)] out ParsedFrame? parsedFrame)
    {
        parsedFrame = null;
        var tree = StackFrameParser.TryParse(line);

        if (tree is null)
        {
            return false;
        }

        parsedFrame = new ParsedStackFrame(tree);
        return true;
    }
}
