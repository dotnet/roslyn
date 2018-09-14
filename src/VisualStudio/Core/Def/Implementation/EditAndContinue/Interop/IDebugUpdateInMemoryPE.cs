// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.CodeAnalysis.EditAndContinue;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct LINEUPDATE
    {
        public uint Line;
        public uint UpdatedLine;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LINEDELTA
    {
        public uint MethodToken;
        public int Delta;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FILEUPDATE
    {
        [MarshalAs(UnmanagedType.BStr)]
        public string FileName;

        /// <summary>
        /// This is really an pointer to an array of "cLineUpdate" LINEUPDATE struct.
        /// </summary>
        public IntPtr LineUpdates;

        public uint LineUpdateCount;
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("9E2BD568-7CEE-4166-ABC9-495BA8D3054A")]
    internal interface IDebugUpdateInMemoryPE
    {
        void GetMetadataEmit([MarshalAs(UnmanagedType.IUnknown)]out object ppMetadataEmit);

        // The compiler must provide updated IL for modified
        // methods.
        void SetDeltaIL([In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]byte[] pbIL, uint cbIL);

        // This stream holds the pdb output of recompiling
        // modified methods.  See ISymUnmanagedWriter.
        void SetDeltaPdb(IStream pDeltaPdbStream);

        // The LangSvc is required to provide line information
        // for non-recompiled methods that have moved source location.
        // Any method where line movement cannot be represented
        // by a single delta must be recompiled and represented in
        // the delta pdb.
        void SetDeltaLines([In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]LINEDELTA[] pMethodLocationDeltas, uint cMethodLocationDeltas);

        // This method is used to determine the current slot
        // location of local variables.
        void GetENCDebugInfo(out IENCDebugInfo ppDebugInfo);

        // This method provides the debugger with the tokens of methods
        // that have been compiled in the update.
        void SetRemapMethods([In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]int[] pmdRemapMethodTokens, uint cRemapMethods);

        // This is an alternative to SetDeltaLines.  It is subject to
        // the same restriction as SetDeltaLines in that it can only update
        // whole methods.  It is provided for lang services that cannot 
        // determine the method tokens for all methods in a file.
        // The lineUpdates array in each FILEUPDATE provides the line from the
        // last version of the file and where it is in the new version of the file.
        // Both lines, and lineUpdates must be in monotonically increasing order
        // within the array.
        void SetFileUpdates([In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]FILEUPDATE[] pFileUpdates, [In]uint cFileUpdates);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("8A9E5AAE-BEF6-47A8-879B-690463516D73")]
    internal interface IDebugUpdateInMemoryPE2 : IDebugUpdateInMemoryPE
    {
        // leave a vtable gap for IDebugUpdateInMemoryPE methods
        void _VtblGap0_7();

        void GetMetadataByteCount(out uint cb);

        void GetMetadataBytes(uint cb, IntPtr pbMetadata, out uint cbFetched);

        void SetDeltaMetadata([In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]byte[] pbMetadata, uint cbMetadata);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("AE2FF3A4-2FA6-487C-AE74-9FB3D9276742")]
    internal interface IDebugUpdateInMemoryPE3 : IDebugUpdateInMemoryPE2
    {
        // leave a vtable gap for IDebugUpdateInMemoryPE2 methods
        void _VtblGap0_10();

        void SetExceptionRanges([In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]ENCPROG_EXCEPTION_RANGE[] pRanges, int cRanges);
        void SetRemapActiveStatements([In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]ENCPROG_ACTIVE_STATEMENT_REMAP[] pRemapActiveStatements, int cRemapActiveStatements);
    }
}
