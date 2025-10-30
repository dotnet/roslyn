// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;

internal readonly struct AspNetCoreVirtualChar : IEquatable<AspNetCoreVirtualChar>
{
    internal readonly VirtualChar VirtualChar;

    internal AspNetCoreVirtualChar(VirtualChar virtualChar)
    {
        VirtualChar = virtualChar;
    }

    /// <summary>
    /// Returns the Unicode scalar value as an integer.
    /// </summary>
    public int RuneValue
    {
        // Rune is an internal shim with netstandard2.0 and accessing it throws an internal access exception.
        // Expose integer value. Can be converted back to Rune by caller.
        get => char.IsSurrogate(VirtualChar) ? Rune.ReplacementChar.Value : VirtualChar;
    }

    public char SurrogateChar => char.IsSurrogate(VirtualChar) ? VirtualChar : '\0';

    /// <inheritdoc cref="VirtualChar.Span"/>
    public TextSpan Span => VirtualChar.Span;

    /// <inheritdoc cref="VirtualChar.Value"/>
    public int Value => VirtualChar.Value;

    /// <inheritdoc cref="VirtualChar.ToString"/>
    public override string ToString() => VirtualChar.ToString();

    /// <inheritdoc cref="VirtualChar.Equals(object)"/>
    public override bool Equals(object? obj) => obj is AspNetCoreVirtualChar vc && Equals(vc);

    /// <inheritdoc cref="VirtualChar.Equals(VirtualChar)"/>
    public bool Equals(AspNetCoreVirtualChar other) => VirtualChar.Equals(other.VirtualChar);

    /// <inheritdoc cref="VirtualChar.GetHashCode"/>
    public override int GetHashCode() => VirtualChar.GetHashCode();
}
