// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
