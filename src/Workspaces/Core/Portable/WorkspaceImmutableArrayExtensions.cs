// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    internal static partial class ImmutableArrayExtensions
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct ImmutableArrayProxy<T>
        {
            internal T[] MutableArray;
        }

        internal static ImmutableArray<T> DangerousCreateFromUnderlyingArray<T>(ref T[] array)
        {
            var proxy = new ImmutableArrayProxy<T> { MutableArray = array };
            array = null;
            return Unsafe.As<ImmutableArrayProxy<T>, ImmutableArray<T>>(ref proxy);
        }
    }
}
