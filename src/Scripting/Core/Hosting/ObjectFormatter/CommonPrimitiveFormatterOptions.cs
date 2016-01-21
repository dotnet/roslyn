// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public struct CommonPrimitiveFormatterOptions
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

        public CommonPrimitiveFormatterOptions(int numberRadix, bool includeCodePoints, bool quoteStringsAndCharacters, bool escapeNonPrintableCharacters)
        {
            NumberRadix = numberRadix;
            IncludeCharacterCodePoints = includeCodePoints;
            QuoteStringsAndCharacters = quoteStringsAndCharacters;
            EscapeNonPrintableCharacters = escapeNonPrintableCharacters;
        }
    }
}
