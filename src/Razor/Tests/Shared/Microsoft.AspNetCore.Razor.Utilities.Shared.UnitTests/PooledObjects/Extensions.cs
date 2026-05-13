// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.PooledObjects;

internal static class Extensions
{
    public static void Validate<T>(this ref readonly PooledArrayBuilder<T> builder, Action<PooledArrayBuilder<T>.TestAccessor> validator)
    {
        validator(builder.GetTestAccessor());
    }
}
