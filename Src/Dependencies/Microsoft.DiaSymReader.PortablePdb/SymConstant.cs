// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(true)]
    public sealed class SymConstant : ISymUnmanagedConstant
    {
        public int GetName(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]char[] name)
        {
            throw new NotImplementedException();
        }

        public int GetSignature(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]byte[] signature)
        {
            throw new NotImplementedException();
        }

        public int GetValue(out object value)
        {
            throw new NotImplementedException();
        }
    }
}
