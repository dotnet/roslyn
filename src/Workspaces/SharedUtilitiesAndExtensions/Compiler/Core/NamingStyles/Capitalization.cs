// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal enum Capitalization : byte
    {
        /// <summary>
        /// Each word is capitalized
        /// </summary>
        PascalCase,

        /// <summary>
        /// Every word except the first word is capitalized
        /// </summary>
        CamelCase,

        /// <summary>
        /// Only the first word is capitalized
        /// </summary>
        FirstUpper,

        /// <summary>
        /// Every character is capitalized
        /// </summary>
        AllUpper,

        /// <summary>
        /// No characters are capitalized
        /// </summary>
        AllLower
    }
}
