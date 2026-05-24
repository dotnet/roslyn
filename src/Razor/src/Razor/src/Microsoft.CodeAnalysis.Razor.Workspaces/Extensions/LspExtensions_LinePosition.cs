// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Roslyn.LanguageServer.Protocol;

internal static partial class LspExtensions
{
    public static Position ToPosition(this LinePosition linePosition)
        => LspFactory.CreatePosition(linePosition.Line, linePosition.Character);

    public static LspRange ToZeroWidthRange(this LinePosition position)
        => LspFactory.CreateZeroWidthRange(position);
}
