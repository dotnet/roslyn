// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal static class ArrayBuilderExtensions
    {
        public static bool Any<T>(this ArrayBuilder<T> builder, Func<T, bool> predicate)
        {
            foreach (var item in builder)
            {
                if (predicate(item))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool All<T>(this ArrayBuilder<T> builder, Func<T, bool> predicate)
        {
            foreach (var item in builder)
            {
                if (!predicate(item))
                {
                    return false;
                }
            }
            return true;
        }

        public static ImmutableArray<U> SelectAsArray<T, U>(this ArrayBuilder<T> builder, Func<T, U> map)
        {
            int n = builder.Count;
            if (n == 0)
            {
                return ImmutableArray<U>.Empty;
            }
            else
            {
                var result = new U[n];
                for (int i = 0; i < n; i++)
                {
                    result[i] = map(builder[i]);
                }
                return result.AsImmutableOrNull();
            }
        }

        public static void AddOptional<T>(this ArrayBuilder<T> builder, T item)
            where T : class
        {
            if (item != null)
            {
                builder.Add(item);
            }
        }

        // The following extension methods allow an ArrayBuilder to be used as a stack. 
        // Note that the order of an IEnumerable from a List is from bottom to top of stack. An IEnumerable 
        // from the framework Stack is from top to bottom.
        public static void Push<T>(this ArrayBuilder<T> builder, T e)
        {
            builder.Add(e);
        }

        public static T Pop<T>(this ArrayBuilder<T> builder)
        {
            var e = builder.Peek();
            builder.RemoveAt(builder.Count - 1);
            return e;
        }

        public static T Peek<T>(this ArrayBuilder<T> builder)
        {
            return builder[builder.Count - 1];
        }
    }
}
