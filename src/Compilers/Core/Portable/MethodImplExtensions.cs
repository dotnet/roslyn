// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis;

internal static class MethodImplAttributeExtensions
{
    extension(MethodImplAttributes)
    {
        public static MethodImplAttributes Async
        {
            get
            {
#if NET10_0_OR_GREATER
                Debug.Assert(MethodImplAttributes.Async == (MethodImplAttributes)0x2000);
#endif
                return (MethodImplAttributes)0x2000;
            }
        }
    }
}

internal static class MethodImplOptionsExtensions
{
    extension(MethodImplOptions)
    {
        public static MethodImplOptions Async
        {
            get
            {
#if NET10_0_OR_GREATER
                Debug.Assert(MethodImplOptions.Async == (MethodImplOptions)0x2000);
#endif
                return (MethodImplOptions)0x2000;
            }
        }
    }
}
