// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Reflection;

namespace Microsoft.Cci
{
    /// <summary>
    /// Represents a file referenced by an assembly.
    /// </summary>
    internal interface IFileReference
    {
        /// <summary>
        /// True if the file has metadata.
        /// </summary>
        bool HasMetadata { get; }

        /// <summary>
        /// File name with extension.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// A hash of the file contents.
        /// </summary>
        ImmutableArray<byte> GetHashValue(AssemblyHashAlgorithm algorithmId);
    }
}
