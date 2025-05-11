// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal static class StandardCallbacks<T>
{
    public static readonly Action<T, HashSet<T>> AddToHashSet =
        static (data, set) => set.Add(data);

    public static readonly Action<T, ArrayBuilder<T>> AddToArrayBuilder =
        static (data, builder) => builder.Add(data);
}
