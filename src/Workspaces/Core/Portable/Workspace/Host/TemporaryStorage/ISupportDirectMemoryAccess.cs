// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
