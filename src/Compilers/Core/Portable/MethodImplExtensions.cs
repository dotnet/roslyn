// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis;

internal static class MethodImplAttributeExtensions
{
    extension(MethodImplAttributes)
    {
        public static MethodImplAttributes Async =>
#if NET10_0_OR_GREATER
            MethodImplAttributes.Async;
#else
            (MethodImplAttributes)0x2000;
#endif
    }
}

internal static class MethodImplOptionsExtensions
{
    extension(MethodImplOptions)
    {
        public static MethodImplOptions Async =>
#if NET10_0_OR_GREATER
            MethodImplOptions.Async;
#else
            (MethodImplOptions)0x2000;
#endif
    }
}
