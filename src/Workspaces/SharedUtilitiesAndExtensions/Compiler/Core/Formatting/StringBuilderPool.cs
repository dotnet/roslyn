// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class StringBuilderPool
{
    public static StringBuilder Allocate()
        => SharedPools.Default<StringBuilder>().AllocateAndClear();

    public static void Free(StringBuilder builder)
        => SharedPools.Default<StringBuilder>().ClearAndFree(builder);

    public static string ReturnAndFree(StringBuilder builder)
    {
        SharedPools.Default<StringBuilder>().ForgetTrackedObject(builder);
        return builder.ToString();
    }
}
