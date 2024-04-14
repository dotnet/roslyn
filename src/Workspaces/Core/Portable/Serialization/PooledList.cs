// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// This is just internal utility type to reduce allocations and redundant code
/// </summary>
internal static class Creator
{
    public static PooledObject<List<T>> CreateList<T>()
        => SharedPools.Default<List<T>>().GetPooledObject();
}
