// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

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
