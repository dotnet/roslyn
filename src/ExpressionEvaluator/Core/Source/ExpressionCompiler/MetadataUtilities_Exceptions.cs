// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static partial class MetadataUtilities
    {
        internal const uint COR_E_BADIMAGEFORMAT = 0x8007000b;
        internal const uint CORDBG_E_MISSING_METADATA = 0x80131c35;

        internal static bool IsBadOrMissingMetadataException(Exception e, string moduleName)
        {
            RoslynDebug.AssertNotNull(moduleName);

            if (e is ObjectDisposedException)
            {
                Debug.WriteLine($"Module '{moduleName}' has been disposed.");
                return true;
            }

            switch (unchecked((uint)e.HResult))
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
    }
}
