// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CodeAnalysis.Text;

internal static class LinePositionExtensions
{
    public static void Deconstruct(this LinePosition linePosition, out int line, out int character)
        => (line, character) = (linePosition.Line, linePosition.Character);

    public static LinePositionSpan ToZeroWidthSpan(this LinePosition linePosition)
        => new(linePosition, linePosition);

    public static LinePosition WithLine(this LinePosition linePosition, int newLine)
        => new(newLine, linePosition.Character);

    public static LinePosition WithLine(this LinePosition linePosition, Func<int, int> computeNewLine)
        => new(computeNewLine(linePosition.Line), linePosition.Character);

    public static LinePosition WithCharacter(this LinePosition linePosition, int newCharacter)
        => new(linePosition.Line, newCharacter);

    public static LinePosition WithCharacter(this LinePosition linePosition, Func<int, int> computeNewCharacter)
        => new(linePosition.Line, computeNewCharacter(linePosition.Character));
}
