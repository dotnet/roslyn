﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections.Internal
{
    /// <summary>
    /// Provides static methods to invoke <see cref="IEnumerable{T}"/> members on value types that explicitly implement
    /// the member.
    /// </summary>
    /// <remarks>
    /// Normally, invocation of explicit interface members requires boxing or copying the value type, which is
    /// especially problematic for operations that mutate the value. Invocation through these helpers behaves like a
    /// normal call to an implicitly implemented member.
    /// </remarks>
    internal static class IEnumerableCalls<T>
    {
        public static IEnumerator<T> GetEnumerator<TEnumerable>(ref TEnumerable enumerable)
            where TEnumerable : IEnumerable<T>
        {
            return enumerable.GetEnumerator();
        }
    }
}
