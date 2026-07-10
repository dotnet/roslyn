// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#pragma warning disable 436 // SuppressUnmanagedCodeSecurityAttribute defined in source and mscorlib

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.DiaSymReader
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("B01FAFEB-C450-3A4D-BEEC-B4CEEC01E006"), SuppressUnmanagedCodeSecurity]
    [GeneratedWhenPossibleComInterface]
    internal unsafe partial interface ISymUnmanagedDocumentWriter
    {
        // Roslyn uses byte* instead of byte[] (upstream) to avoid allocations when passing ReadOnlySpan<byte>.
        void SetSource(uint sourceSize, byte* source);
        void SetCheckSum(Guid algorithmId, uint checkSumSize, byte* checkSum);
    }
}
