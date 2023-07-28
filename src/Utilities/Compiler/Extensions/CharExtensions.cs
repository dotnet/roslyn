// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.Extensions
{
    internal static class CharExtensions
    {
        // From: https://github.com/dotnet/runtime/blob/d1fc57ea18ee90aee8690697caed2b9f162409eb/src/libraries/System.Private.CoreLib/src/System/Char.cs#L91
        public static bool IsAscii(this char c) => (uint)c <= '\x007f';

        /// <summary>
        /// Returns whether the char is a printable ascii character [x0020, x007e].
        /// </summary>
        public static bool IsPrintableAscii(this char c) => (uint)c is >= '\x0020' and <= '\x007e';
    }
}
