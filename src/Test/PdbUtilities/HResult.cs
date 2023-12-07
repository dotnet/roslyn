// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.Test.Utilities
{
    public static class HResult
    {
        public const int S_OK = 0x0;
        public const int S_FALSE = 0x1;
        public const int E_FAIL = unchecked((int)0x80004005);
        public const int E_NOTIMPL = unchecked((int)0x80004001);
    }
}
