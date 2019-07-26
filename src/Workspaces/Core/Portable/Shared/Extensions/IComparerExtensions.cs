// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class IComparerExtensions
    {
        public static IComparer<T> Inverse<T>(this IComparer<T> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return new InverseComparer<T>(comparer);
        }

        private class InverseComparer<T> : IComparer<T>
        {
            private readonly IComparer<T> _comparer;

            internal InverseComparer(IComparer<T> comparer)
            {
                _comparer = comparer;
            }

            public int Compare(T x, T y)
            {
                return _comparer.Compare(y, x);
            }
        }
    }
}
