// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Class which represents a position on a text document.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#position">Language Server Protocol specification</see> for additional information.
/// </summary>
internal sealed class Position : IEquatable<Position>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Position"/> class.
    /// </summary>
    public Position()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Position"/> class.
    /// </summary>
    /// <param name="line">Line number.</param>
    /// <param name="character">Character number.</param>
    public Position(int line, int character)
    {
        this.Line = line;
        this.Character = character;
    }

    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    [JsonPropertyName("line")]
    public int Line
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the character number.
    /// </summary>
    [JsonPropertyName("character")]
    public int Character
    {
        get;
        set;
    }

    /// <summary>
    /// Overrides default equals operator.  Two positions are equal if they are both null or one of them is the object equivalent of the other.
    /// </summary>
    /// <param name="firstPosition">The first position to compare.</param>
    /// <param name="secondPosition">The second position to compare.</param>
    /// <returns>True if both positions are null or one of them is the object equivalent of the other, false otherwise.</returns>
    public static bool operator ==(Position? firstPosition, Position? secondPosition)
    {
        if (firstPosition is null && secondPosition is null)
        {
            return true;
        }

        if (firstPosition is null && secondPosition is not null)
        {
            return false;
        }

        if (firstPosition is not null && secondPosition is null)
        {
            return false;
        }

        return firstPosition!.Equals(secondPosition!);
    }

    /// <summary>
    /// Overrides the default not equals operator.
    /// </summary>
    /// <param name="firstPosition">The first position to compare.</param>
    /// <param name="secondPosition">The second position to compare.</param>
    /// <returns>True if first and second positions are not equivalent.</returns>
    public static bool operator !=(Position? firstPosition, Position? secondPosition)
    {
        return !(firstPosition == secondPosition);
    }

    /// <summary>
    /// Overrides base class method <see cref="object.Equals(object)"/>. Two positions are equal if their line and character are the same.
    /// </summary>
    /// <param name="obj">Object to compare to.</param>
    /// <returns>True if the given position has the same line and character; false otherwise.</returns>
    public override bool Equals(object obj)
    {
        return this.Equals(obj as Position);
    }

    /// <inheritdoc/>
    public bool Equals(Position? other)
    {
        return other != null &&
               this.Line == other.Line &&
               this.Character == other.Character;
    }

    /// <summary>
    /// Overrides base class method <see cref="object.GetHashCode()"/>.
    /// </summary>
    /// <returns>Hashcode for this object.</returns>
    public override int GetHashCode()
    {
        return this.Line ^ this.Character;
    }
}
