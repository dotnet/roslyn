// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    internal interface IStackFrameParser
    {
        bool TryParseLine(VirtualCharSequence line, [NotNullWhen(returnValue: true)] out ParsedFrame? parsedFrame);
    }
}
