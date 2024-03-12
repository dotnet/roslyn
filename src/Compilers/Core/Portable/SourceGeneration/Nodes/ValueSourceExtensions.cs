// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    public static class IncrementalValueProviderExtensions
    {
        // 1 => 1 transform 
        public static IncrementalValueProvider<TResult> Select<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector) => new IncrementalValueProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector, wrapUserFunc: source.CatchAnalyzerExceptions), source.CatchAnalyzerExceptions);

        public static IncrementalValuesProvider<TResult> Select<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector, wrapUserFunc: source.CatchAnalyzerExceptions), source.CatchAnalyzerExceptions);

        // 1 => many (or none) transform
        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, ImmutableArray<TResult>> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector, wrapUserFunc: source.CatchAnalyzerExceptions), source.CatchAnalyzerExceptions);

        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, IEnumerable<TResult>> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector.WrapUserFunctionAsImmutableArray(source.CatchAnalyzerExceptions)), source.CatchAnalyzerExceptions);

        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, ImmutableArray<TResult>> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector, wrapUserFunc: source.CatchAnalyzerExceptions), source.CatchAnalyzerExceptions);

        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, IEnumerable<TResult>> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector.WrapUserFunctionAsImmutableArray(source.CatchAnalyzerExceptions)), source.CatchAnalyzerExceptions);

        public static IncrementalValueProvider<ImmutableArray<TSource>> Collect<TSource>(this IncrementalValuesProvider<TSource> source) => new IncrementalValueProvider<ImmutableArray<TSource>>(new BatchNode<TSource>(source.Node), source.CatchAnalyzerExceptions);

        public static IncrementalValuesProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(this IncrementalValuesProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2) => new IncrementalValuesProvider<(TLeft, TRight)>(new CombineNode<TLeft, TRight>(provider1.Node, provider2.Node), provider1.CatchAnalyzerExceptions);

        public static IncrementalValueProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(this IncrementalValueProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2) => new IncrementalValueProvider<(TLeft, TRight)>(new CombineNode<TLeft, TRight>(provider1.Node, provider2.Node), provider1.CatchAnalyzerExceptions);

        // helper for filtering
        public static IncrementalValuesProvider<TSource> Where<TSource>(this IncrementalValuesProvider<TSource> source, Func<TSource, bool> predicate) => source.SelectMany((item, _) => predicate(item) ? ImmutableArray.Create(item) : ImmutableArray<TSource>.Empty);

        internal static IncrementalValuesProvider<TSource> Where<TSource>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, bool> predicate) => source.SelectMany((item, c) => predicate(item, c) ? ImmutableArray.Create(item) : ImmutableArray<TSource>.Empty);

        // custom comparer for given node
        public static IncrementalValueProvider<TSource> WithComparer<TSource>(this IncrementalValueProvider<TSource> source, IEqualityComparer<TSource> comparer) => new IncrementalValueProvider<TSource>(source.Node.WithComparer(comparer.WrapUserComparer(source.CatchAnalyzerExceptions)), source.CatchAnalyzerExceptions);

        public static IncrementalValuesProvider<TSource> WithComparer<TSource>(this IncrementalValuesProvider<TSource> source, IEqualityComparer<TSource> comparer) => new IncrementalValuesProvider<TSource>(source.Node.WithComparer(comparer.WrapUserComparer(source.CatchAnalyzerExceptions)), source.CatchAnalyzerExceptions);

        // custom node name for incremental testing support
        public static IncrementalValueProvider<TSource> WithTrackingName<TSource>(this IncrementalValueProvider<TSource> source, string name) => new IncrementalValueProvider<TSource>(source.Node.WithTrackingName(name), source.CatchAnalyzerExceptions);

        public static IncrementalValuesProvider<TSource> WithTrackingName<TSource>(this IncrementalValuesProvider<TSource> source, string name) => new IncrementalValuesProvider<TSource>(source.Node.WithTrackingName(name), source.CatchAnalyzerExceptions);
    }
}
