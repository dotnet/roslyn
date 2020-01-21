// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
