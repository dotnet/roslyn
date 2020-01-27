// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
