// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// support direct memory access pointer
    /// </summary>
    internal interface ISupportDirectMemoryAccess
    {
        IntPtr GetPointer();
    }
}
