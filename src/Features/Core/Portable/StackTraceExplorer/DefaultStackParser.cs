// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.StackTraceExplorer;

internal sealed class DefaultStackParser : IStackFrameParser
{
    public bool TryParseLine(VirtualCharSequence line, [NotNullWhen(true)] out ParsedFrame? parsedFrame)
    {
        // For now we just keep all text so the user can still see lines they pasted and they
        // don't disappear. In the future we might want to restrict what we show.
        parsedFrame = new IgnoredFrame(line);
        return true;
    }
}
