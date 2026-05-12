// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Roslyn.LanguageServer.Protocol;

internal static partial class LspExtensions
{
    public static void Deconstruct(this Position position, out int line, out int character)
        => (line, character) = (position.Line, position.Character);

    public static LinePosition ToLinePosition(this Position position)
        => new(position.Line, position.Character);

    public static LspRange ToZeroWidthRange(this Position position)
        => LspFactory.CreateZeroWidthRange(position);

    public static int CompareTo(this Position position, Position other)
    {
        var result = position.Line.CompareTo(other.Line);
        return result != 0 ? result : position.Character.CompareTo(other.Character);
    }

    public static string ToDisplayString(this Position position)
        => $"({position.Line}, {position.Character})";
}
