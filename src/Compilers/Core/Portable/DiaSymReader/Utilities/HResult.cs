// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.DiaSymReader
{
    internal static class HResult
    {
        internal const int S_OK = 0;
        internal const int S_FALSE = 1;
        internal const int E_NOTIMPL = unchecked((int)0x80004001);
        internal const int E_FAIL = unchecked((int)0x80004005);
        internal const int E_INVALIDARG = unchecked((int)0x80070057);
        internal const int E_UNEXPECTED = unchecked((int)0x8000FFFF);
    }
}
