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
#if !NET
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("B01FAFEB-C450-3A4D-BEEC-B4CEEC01E006"), SuppressUnmanagedCodeSecurity]
#endif
    internal unsafe interface ISymUnmanagedDocumentWriter
    {
#if NET
        public static readonly Guid IID = new Guid("B01FAFEB-C450-3A4D-BEEC-B4CEEC01E006");
#endif
        void SetSource(uint sourceSize, byte* source);
        void SetCheckSum(Guid algorithmId, uint checkSumSize, byte* checkSum);
    }
}
