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
        // https://github.com/dotnet/roslyn/issues/79792: Use the real value when possible
        public static MethodImplAttributes Async => (MethodImplAttributes)0x2000;
    }
}

internal static class MethodImplOptionsExtensions
{
    extension(MethodImplOptions)
    {
        // https://github.com/dotnet/roslyn/issues/79792: Use the real value when possible
        public static MethodImplOptions Async => (MethodImplOptions)0x2000;
    }
}
