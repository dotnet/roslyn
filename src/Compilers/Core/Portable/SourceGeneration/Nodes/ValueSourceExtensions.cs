// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    public static class IncrementalValueProviderExtensions
    {
        // 1 => 1 transform 
        public static IncrementalValueProvider<TResult> Select<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, TResult> selector) => new IncrementalValueProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector.WrapUserFunction()));

        public static IncrementalValuesProvider<TResult> Select<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, TResult> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector.WrapUserFunction()));

        // 1 => many (or none) transform
        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, ImmutableArray<TResult>> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector.WrapUserFunction()));

        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, IEnumerable<TResult>> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, t => selector.WrapUserFunction()(t).ToImmutableArray()));

        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, ImmutableArray<TResult>> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector.WrapUserFunction()));

        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, IEnumerable<TResult>> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, t => selector.WrapUserFunction()(t).ToImmutableArray()));

        public static IncrementalValueProvider<ImmutableArray<TSource>> AsSingleValue<TSource>(this IncrementalValuesProvider<TSource> source) => new IncrementalValueProvider<ImmutableArray<TSource>>(new BatchNode<TSource>(source.Node));

        public static IncrementalValuesProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(this IncrementalValuesProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2) => new IncrementalValuesProvider<(TLeft, TRight)>(new CombineNode<TLeft, TRight>(provider1.Node, provider2.Node));

        public static IncrementalValueProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(this IncrementalValueProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2) => new IncrementalValueProvider<(TLeft, TRight)>(new CombineNode<TLeft, TRight>(provider1.Node, provider2.Node));

        // helper for filtering
        public static IncrementalValuesProvider<TSource> Where<TSource>(this IncrementalValuesProvider<TSource> source, Func<TSource, bool> predicate) => source.SelectMany((item) => predicate(item) ? ImmutableArray.Create(item) : ImmutableArray<TSource>.Empty);

        // custom comparer for given node
        public static IncrementalValueProvider<TSource> WithComparer<TSource>(this IncrementalValueProvider<TSource> source, IEqualityComparer<TSource> comparer) => new IncrementalValueProvider<TSource>(source.Node.WithComparer(comparer));

        public static IncrementalValuesProvider<TSource> WithComparer<TSource>(this IncrementalValuesProvider<TSource> source, IEqualityComparer<TSource> comparer) => new IncrementalValuesProvider<TSource>(source.Node.WithComparer(comparer));
    }
}
