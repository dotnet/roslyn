// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.Cci
{
    /// <summary>
    /// Constants for producing and consuming streams of binary custom debug info.
    /// </summary>
    internal static class CustomDebugInfoConstants
    {
        // The version number of the custom debug info binary format.
        // CDIVERSION in Dev10
        internal const int CdiVersion = 4;

        // The number of bytes at the beginning of the byte array that contain global header information.
        // start after header (version byte + size byte + dword padding)
        internal const int CdiGlobalHeaderSize = 4;

        // The number of bytes at the beginning of each custom debug info record that contain header information
        // common to all record types (i.e. byte, kind, size).
        // version byte + kind byte + two bytes padding + size dword
        internal const int CdiRecordHeaderSize = 8;

        internal const byte CdiKindUsingInfo = 0;
        internal const byte CdiKindForwardInfo = 1;
        internal const byte CdiKindForwardToModuleInfo = 2;
        internal const byte CdiKindStateMachineHoistedLocalScopes = 3;
        internal const byte CdiKindForwardIterator = 4;
        internal const byte CdiKindDynamicLocals = 5;
        internal const byte CdiKindEditAndContinueLocalSlotMap = 6;
        internal const byte CdiKindEditAndContinueLambdaMap = 7;
    }
}
