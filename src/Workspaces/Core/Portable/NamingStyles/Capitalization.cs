// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal enum Capitalization
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