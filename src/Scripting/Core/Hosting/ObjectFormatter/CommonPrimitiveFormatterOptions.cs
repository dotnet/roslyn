// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Globalization;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal readonly struct CommonPrimitiveFormatterOptions
    {
        /// <remarks>
        /// Since <see cref="CommonPrimitiveFormatter"/> is an extension point, we don't
        /// perform any validation on <see cref="NumberRadix"/> - it's up to the individual
        /// subtype.
        /// </remarks>
        public int NumberRadix { get; }
        public bool IncludeCharacterCodePoints { get; }
        public bool QuoteStringsAndCharacters { get; }
        public bool EscapeNonPrintableCharacters { get; }
        public CultureInfo CultureInfo { get; }

        public CommonPrimitiveFormatterOptions(
            int numberRadix,
            bool includeCodePoints,
            bool quoteStringsAndCharacters,
            bool escapeNonPrintableCharacters,
            CultureInfo cultureInfo)
        {
            NumberRadix = numberRadix;
            IncludeCharacterCodePoints = includeCodePoints;
            QuoteStringsAndCharacters = quoteStringsAndCharacters;
            EscapeNonPrintableCharacters = escapeNonPrintableCharacters;
            CultureInfo = cultureInfo;
        }
    }
}
