// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages
{
    internal readonly struct AspNetCoreVirtualChar
    {
        private readonly VirtualChar _virtualChar;

        internal AspNetCoreVirtualChar(VirtualChar virtualChar)
        {
            _virtualChar = virtualChar;
        }

        /// <inheritdoc cref="VirtualChar.Rune"/>
        public Rune Rune => _virtualChar.Rune;

        /// <inheritdoc cref="VirtualChar.SurrogateChar"/>
        public char SurrogateChar => _virtualChar.SurrogateChar;

        /// <inheritdoc cref="VirtualChar.Span"/>
        public TextSpan Span => _virtualChar.Span;

        /// <inheritdoc cref="VirtualChar.Value"/>
        public int Value => _virtualChar.Value;

        /// <inheritdoc cref="VirtualChar.ToString"/>
        public override string ToString() => _virtualChar.ToString();
    }
}
