// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable RS0062 // Do not implicitly capture primary constructor parameters

namespace Roslyn.Test.Utilities
{
    public class TestEqualityComparer<T>(Func<T?, T?, bool>? equals = null, Func<T, int>? getHashCode = null) : IEqualityComparer<T>
    {
        public bool Equals(T? x, T? y)
            => (equals ?? EqualityComparer<T>.Default.Equals)(x, y);

        public int GetHashCode([DisallowNull] T obj)
            => (getHashCode ?? EqualityComparer<T>.Default.GetHashCode!)(obj);
    }
}
