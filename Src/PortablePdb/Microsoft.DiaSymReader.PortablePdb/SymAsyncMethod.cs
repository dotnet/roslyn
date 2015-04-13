// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    [ComVisible(false)]
    public sealed class SymAsyncMethod : ISymUnmanagedAsyncMethod
    {
        public int GetAsyncStepInfo(
            int bufferLength, 
            out int count, 
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]int[] yieldOffsets,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]int[] breakpointOffset, 
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]int[] breakpointMethod)
        {
            throw new NotImplementedException();
        }

        public int GetAsyncStepInfoCount(out int count)
        {
            throw new NotImplementedException();
        }

        public int GetCatchHandlerILOffset(out int offset)
        {
            throw new NotImplementedException();
        }

        public int GetKickoffMethod(out int kickoffMethodToken)
        {
            throw new NotImplementedException();
        }

        public int HasCatchHandlerILOffset(out bool offset)
        {
            throw new NotImplementedException();
        }

        public int IsAsyncMethod(out bool value)
        {
            throw new NotImplementedException();
        }
    }
}
