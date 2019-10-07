// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

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

        public static bool Any<T, A>(this ArrayBuilder<T> builder, Func<T, A, bool> predicate, A arg)
        {
            foreach (var item in builder)
            {
                if (predicate(item, arg))
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

        public static bool All<T, A>(this ArrayBuilder<T> builder, Func<T, A, bool> predicate, A arg)
        {
            foreach (var item in builder)
            {
                if (!predicate(item, arg))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Maps an array builder to immutable array.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="items">The array to map</param>
        /// <param name="map">The mapping delegate</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array</returns>
        public static ImmutableArray<TResult> SelectAsArray<TItem, TResult>(this ArrayBuilder<TItem> items, Func<TItem, TResult> map)
        {
            switch (items.Count)
            {
                case 0:
                    return ImmutableArray<TResult>.Empty;

                case 1:
                    return ImmutableArray.Create(map(items[0]));

                case 2:
                    return ImmutableArray.Create(map(items[0]), map(items[1]));

                case 3:
                    return ImmutableArray.Create(map(items[0]), map(items[1]), map(items[2]));

                case 4:
                    return ImmutableArray.Create(map(items[0]), map(items[1]), map(items[2]), map(items[3]));

                default:
                    var builder = ArrayBuilder<TResult>.GetInstance(items.Count);
                    foreach (var item in items)
                    {
                        builder.Add(map(item));
                    }

                    return builder.ToImmutableAndFree();
            }
        }

        /// <summary>
        /// Maps an array builder to immutable array.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TArg"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="items">The sequence to map</param>
        /// <param name="map">The mapping delegate</param>
        /// <param name="arg">The extra input used by mapping delegate</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> SelectAsArray<TItem, TArg, TResult>(this ArrayBuilder<TItem> items, Func<TItem, TArg, TResult> map, TArg arg)
        {
            switch (items.Count)
            {
                case 0:
                    return ImmutableArray<TResult>.Empty;

                case 1:
                    return ImmutableArray.Create(map(items[0], arg));

                case 2:
                    return ImmutableArray.Create(map(items[0], arg), map(items[1], arg));

                case 3:
                    return ImmutableArray.Create(map(items[0], arg), map(items[1], arg), map(items[2], arg));

                case 4:
                    return ImmutableArray.Create(map(items[0], arg), map(items[1], arg), map(items[2], arg), map(items[3], arg));

                default:
                    var builder = ArrayBuilder<TResult>.GetInstance(items.Count);
                    foreach (var item in items)
                    {
                        builder.Add(map(item, arg));
                    }

                    return builder.ToImmutableAndFree();
            }
        }

        public static void AddOptional<T>(this ArrayBuilder<T> builder, T? item)
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

        public static ImmutableArray<T> ToImmutableOrEmptyAndFree<T>(this ArrayBuilder<T>? builder)
        {
            return builder?.ToImmutableAndFree() ?? ImmutableArray<T>.Empty;
        }

        public static void AddIfNotNull<T>(this ArrayBuilder<T> builder, T? value)
            where T : struct
        {
            if (value != null)
            {
                builder.Add(value.Value);
            }
        }

        public static void AddIfNotNull<T>(this ArrayBuilder<T> builder, T? value)
            where T : class
        {
            if (value != null)
            {
                builder.Add(value);
            }
        }

#nullable disable
        public static void FreeAll<T>(this ArrayBuilder<T> builder, Func<T, ArrayBuilder<T>> getNested)
        {
            foreach (var item in builder)
            {
                getNested(item)?.FreeAll(getNested);
            }
            builder.Free();
        }
#nullable enable
    }
}
