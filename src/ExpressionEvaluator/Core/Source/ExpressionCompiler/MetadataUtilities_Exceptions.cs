// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static partial class MetadataUtilities
    {
        internal const uint COR_E_BADIMAGEFORMAT = 0x8007000b;
        internal const uint CORDBG_E_MISSING_METADATA = 0x80131c35;

        internal static bool IsBadOrMissingMetadataException(Exception e, string moduleName)
        {
            Debug.Assert(moduleName != null);
            if (e is ObjectDisposedException objectDisposed)
            {
                Debug.WriteLine($"Module '{moduleName}' has been disposed.");
                return true;
            }
            switch (GetHResult(e))
            {
                case COR_E_BADIMAGEFORMAT:
                    Debug.WriteLine($"Module '{moduleName}' contains corrupt metadata.");
                    return true;
                case CORDBG_E_MISSING_METADATA:
                    Debug.WriteLine($"Module '{moduleName}' is missing metadata.");
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBadMetadataException(Exception e)
        {
            return GetHResult(e) == COR_E_BADIMAGEFORMAT;
        }

        private static uint GetHResult(Exception e)
        {
            return unchecked((uint)e.HResult);
        }
    }
}
