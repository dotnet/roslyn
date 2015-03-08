// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(true)]
    public sealed class SymVariable : ISymUnmanagedVariable
    {
        public int GetAddressField1(out int value)
        {
            throw new NotImplementedException();
        }

        public int GetAddressField2(out int value)
        {
            throw new NotImplementedException();
        }

        public int GetAddressField3(out int value)
        {
            throw new NotImplementedException();
        }

        public int GetAddressKind(out int kind)
        {
            throw new NotImplementedException();
        }

        public int GetAttributes(out int attributes)
        {
            throw new NotImplementedException();
        }

        public int GetEndOffset(out int offset)
        {
            throw new NotImplementedException();
        }

        public int GetName(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]char[] name)
        {
            throw new NotImplementedException();
        }

        public int GetSignature(int bufferLength, out int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out]byte[] signature)
        {
            throw new NotImplementedException();
        }

        public int GetStartOffset(out int offset)
        {
            throw new NotImplementedException();
        }
    }
}
