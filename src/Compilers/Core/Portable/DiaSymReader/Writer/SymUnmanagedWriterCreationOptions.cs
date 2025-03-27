// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.DiaSymReader
{
    /// <summary>
    /// <see cref="SymUnmanagedWriter"/> creation options.
    /// </summary>
    [Flags]
    internal enum SymUnmanagedWriterCreationOptions
    {
        /// <summary>
        /// Default options.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Use environment variable MICROSOFT_DIASYMREADER_NATIVE_ALT_LOAD_PATH to locate Microsoft.DiaSymReader.Native.{platform}.dll.
        /// </summary>
        UseAlternativeLoadPath = 1 << 1,

        /// <summary>
        /// Use COM registry to locate an implementation of the writer.
        /// </summary>
        UseComRegistry = 1 << 2,

        /// <summary>
        /// Create a deterministic PDB writer.
        /// </summary>
        Deterministic = 1 << 3,
    }
}
