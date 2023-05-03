// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    public static class IncrementalValueProviderExtensions
    {
        // 1 => 1 transform 
        public static IncrementalValueProvider<TResult> Select<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector)
        {
            var wrappedUserFunction = source.Node.TransformFactory.WrapUserFunction(selector);
            var transformNode = source.Node.TransformFactory.Select(source.Node, wrappedUserFunction);
            return new IncrementalValueProvider<TResult>(transformNode);
        }

        public static IncrementalValuesProvider<TResult> Select<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector)
        {
            var wrappedUserFunction = source.Node.TransformFactory.WrapUserFunction(selector);
            var transformNode = source.Node.TransformFactory.Select(source.Node, wrappedUserFunction);
            return new IncrementalValuesProvider<TResult>(transformNode);
        }

        // 1 => many (or none) transform
        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, ImmutableArray<TResult>> selector)
        {
            var wrappedUserFunction = source.Node.TransformFactory.WrapUserFunction(selector);
            var transformNode = source.Node.TransformFactory.SelectMany(source.Node, wrappedUserFunction);
            return new IncrementalValuesProvider<TResult>(transformNode);
        }

        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, IEnumerable<TResult>> selector)
        {
            var wrappedUserFunctionAsImmutableArray = source.Node.TransformFactory.WrapUserFunctionAsImmutableArray(selector);
            var transformNode = source.Node.TransformFactory.SelectMany(source.Node, wrappedUserFunctionAsImmutableArray);
            return new IncrementalValuesProvider<TResult>(transformNode);
        }

        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, ImmutableArray<TResult>> selector)
        {
            var wrappedUserFunction = source.Node.TransformFactory.WrapUserFunction(selector);
            var transformNode = source.Node.TransformFactory.SelectMany(source.Node, wrappedUserFunction);
            return new IncrementalValuesProvider<TResult>(transformNode);
        }

        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, IEnumerable<TResult>> selector)
        {
            var wrappedUserFunctionAsImmutableArray = source.Node.TransformFactory.WrapUserFunctionAsImmutableArray(selector);
            var transformNode = source.Node.TransformFactory.SelectMany(source.Node, wrappedUserFunctionAsImmutableArray);
            return new IncrementalValuesProvider<TResult>(transformNode);
        }

        public static IncrementalValueProvider<ImmutableArray<TSource>> Collect<TSource>(this IncrementalValuesProvider<TSource> source)
        {
            var batchNode = source.Node.TransformFactory.Collect(source.Node);
            return new IncrementalValueProvider<ImmutableArray<TSource>>(batchNode);
        }

        public static IncrementalValuesProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(this IncrementalValuesProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2)
        {
            var combineNode = provider1.Node.TransformFactory.Combine(provider1.Node, provider2.Node);
            return new IncrementalValuesProvider<(TLeft, TRight)>(combineNode);
        }

        public static IncrementalValueProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(this IncrementalValueProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2)
        {
            var combineNode = provider1.Node.TransformFactory.Combine(provider1.Node, provider2.Node);
            return new IncrementalValueProvider<(TLeft, TRight)>(combineNode);
        }

        // helper for filtering
        public static IncrementalValuesProvider<TSource> Where<TSource>(this IncrementalValuesProvider<TSource> source, Func<TSource, bool> predicate)
        {
            var selectManyForFilter = source.Node.TransformFactory.WrapPredicateForSelectMany(predicate);
            return source.SelectMany(selectManyForFilter);
        }

        internal static IncrementalValuesProvider<TSource> Where<TSource>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, bool> predicate)
        {
            var selectManyForFilter = source.Node.TransformFactory.WrapPredicateForSelectMany(predicate);
            return source.SelectMany(selectManyForFilter);
        }

        // custom comparer for given node
        public static IncrementalValueProvider<TSource> WithComparer<TSource>(this IncrementalValueProvider<TSource> source, IEqualityComparer<TSource> comparer)
        {
            var wrappedComparer = source.Node.TransformFactory.WrapUserComparer(comparer);
            return new IncrementalValueProvider<TSource>(source.Node.WithComparer(wrappedComparer));
        }

        public static IncrementalValuesProvider<TSource> WithComparer<TSource>(this IncrementalValuesProvider<TSource> source, IEqualityComparer<TSource> comparer)
        {
            var wrappedComparer = source.Node.TransformFactory.WrapUserComparer(comparer);
            return new IncrementalValuesProvider<TSource>(source.Node.WithComparer(wrappedComparer));
        }

        // custom node name for incremental testing support
        public static IncrementalValueProvider<TSource> WithTrackingName<TSource>(this IncrementalValueProvider<TSource> source, string name) => new IncrementalValueProvider<TSource>(source.Node.WithTrackingName(name));

        public static IncrementalValuesProvider<TSource> WithTrackingName<TSource>(this IncrementalValuesProvider<TSource> source, string name) => new IncrementalValuesProvider<TSource>(source.Node.WithTrackingName(name));
    }
}
