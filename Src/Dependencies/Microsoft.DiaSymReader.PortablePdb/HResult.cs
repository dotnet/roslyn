// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal static class HResult
    {
        internal const int S_OK = 0;
        internal const int S_FALSE = 1;
        internal const int E_NOTIMPL = unchecked((int)0x80004001);
        internal const int E_FAIL = unchecked((int)0x80004005);

        // TODO:
        internal const int E_INVALIDARG = 1;

        // TODO: HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY)
        internal const int E_OUTOFMEMORY = 2;
    }
}
