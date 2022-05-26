// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The kind of metadata a PE file image contains.
    /// </summary>
    public enum MetadataImageKind : byte
    {
        /// <summary>
        /// The PE file is an assembly.
        /// </summary>
        Assembly = 0,

        /// <summary>
        /// The PE file is a module.
        /// </summary>
        Module = 1
    }

    internal static partial class EnumBounds
    {
        internal static bool IsValid(this MetadataImageKind kind)
        {
            return kind >= MetadataImageKind.Assembly && kind <= MetadataImageKind.Module;
        }
    }
}
