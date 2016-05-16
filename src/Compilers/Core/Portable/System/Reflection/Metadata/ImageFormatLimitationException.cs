// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection.Metadata.Ecma335;

#if SRM
namespace System.Reflection.Metadata
#else
namespace Roslyn.Reflection.Metadata
#endif
{
#if SRM
    public
#endif
    class ImageFormatLimitationException : Exception
    {
        private ImageFormatLimitationException(string message)
            : base(message)
        {
        }

        internal static void ThrowHeapSizeLimitExceeded(HeapIndex heapIndex)
        {
            // TODO: localize
            throw new ImageFormatLimitationException($"The limit on the size of #{heapIndex} heap has been exceeded.");
        }
    }
}
