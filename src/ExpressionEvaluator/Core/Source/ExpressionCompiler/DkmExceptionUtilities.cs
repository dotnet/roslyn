// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal static partial class DkmExceptionUtilities
    {
        internal const int COR_E_BADIMAGEFORMAT = unchecked((int)0x8007000b);
        internal const int CORDBG_E_MISSING_METADATA = unchecked((int)0x80131c35);

        internal static bool IsBadOrMissingMetadataException(Exception e)
        {
            return e is ObjectDisposedException ||
                   e.HResult == COR_E_BADIMAGEFORMAT ||
                   e.HResult == CORDBG_E_MISSING_METADATA;
        }
    }
}
