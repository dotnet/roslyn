// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class IComparerExtensions
    {
        public static IComparer<T> Inverse<T>(this IComparer<T> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException("comparer");
            }

            return new InverseComparer<T>(comparer);
        }

        private class InverseComparer<T> : IComparer<T>
        {
            private readonly IComparer<T> comparer;

            internal InverseComparer(IComparer<T> comparer)
            {
                this.comparer = comparer;
            }

            public int Compare(T x, T y)
            {
                return comparer.Compare(y, x);
            }
        }
    }
}