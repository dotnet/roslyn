// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages
{
    internal readonly struct AspNetCoreVirtualChar : IEquatable<AspNetCoreVirtualChar>
    {
        private readonly VirtualChar _virtualChar;

        internal AspNetCoreVirtualChar(VirtualChar virtualChar)
        {
            _virtualChar = virtualChar;
        }

        /// <summary>
        /// Returns the Unicode scalar value as an integer.
        /// </summary>
        public int RuneValue
        {
            // Rune is an internal shim with netstandard2.0 and accessing it throws an internal access exception.
            // Expose integer value. Can be converted back to Rune by caller.
            get => _virtualChar.Rune.Value;
        }

        /// <inheritdoc cref="VirtualChar.SurrogateChar"/>
        public char SurrogateChar => _virtualChar.SurrogateChar;

        /// <inheritdoc cref="VirtualChar.Span"/>
        public TextSpan Span => _virtualChar.Span;

        /// <inheritdoc cref="VirtualChar.Value"/>
        public int Value => _virtualChar.Value;

        /// <inheritdoc cref="VirtualChar.ToString"/>
        public override string ToString() => _virtualChar.ToString();

        /// <inheritdoc cref="VirtualChar.Equals(object)"/>
        public override bool Equals(object? obj) => obj is AspNetCoreVirtualChar vc && Equals(vc);

        /// <inheritdoc cref="VirtualChar.Equals(VirtualChar)"/>
        public bool Equals(AspNetCoreVirtualChar other) => _virtualChar.Equals(other._virtualChar);

        /// <inheritdoc cref="VirtualChar.GetHashCode"/>
        public override int GetHashCode() => _virtualChar.GetHashCode();
    }
}
