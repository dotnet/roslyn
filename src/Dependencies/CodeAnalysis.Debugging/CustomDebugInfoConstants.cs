// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Debugging
{
    /// <summary>
    /// Constants for producing and consuming streams of binary custom debug info.
    /// </summary>
    internal static class CustomDebugInfoConstants
    {
        // The version number of the custom debug info binary format.
        // CDIVERSION in Dev10
        internal const byte Version = 4;

        // The number of bytes at the beginning of the byte array that contain global header information.
        // start after header (version byte + size byte + dword padding)
        internal const int GlobalHeaderSize = 4;

        // The number of bytes at the beginning of each custom debug info record that contain header information
        // common to all record types (i.e. byte, kind, size).
        // version byte + kind byte + two bytes padding + size dword
        internal const int RecordHeaderSize = 8;
    }
}
