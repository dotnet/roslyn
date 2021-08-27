// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies what symbols to import from metadata.
    /// </summary>
    public enum MetadataImportOptions : byte
    {
        /// <summary>
        /// Only import public and protected symbols.
        /// </summary>
        Public = 0,

        /// <summary>
        /// Import public, protected and internal symbols.
        /// </summary>
        Internal = 1,

        /// <summary>
        /// Import all symbols.
        /// </summary>
        All = 2,
    }

    internal static partial class EnumBounds
    {
        internal static bool IsValid(this MetadataImportOptions value)
        {
            return value >= MetadataImportOptions.Public && value <= MetadataImportOptions.All;
        }
    }
}
