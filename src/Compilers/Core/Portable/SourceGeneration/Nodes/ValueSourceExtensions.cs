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

        // many => 1 aggregation
        public static IncrementalValueProvider<ImmutableArray<TSource>> Collect<TSource>(this IncrementalValuesProvider<TSource> source) => new IncrementalValueProvider<ImmutableArray<TSource>>(new BatchNode<TSource>(source.Node), source.CatchAnalyzerExceptions);

        // combine multiple sources together
        public static IncrementalValuesProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(this IncrementalValuesProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2) => new IncrementalValuesProvider<(TLeft, TRight)>(new CombineNode<TLeft, TRight>(provider1.Node, provider2.Node), provider1.CatchAnalyzerExceptions);

        public static IncrementalValuesProvider<(TItem1, TItem2, TItem3)> Combine<TItem1, TItem2, TItem3>(this IncrementalValuesProvider<TItem1> provider1, IncrementalValueProvider<TItem2> provider2, IncrementalValueProvider<TItem3> provider3) => provider1.Combine(provider2).Combine(provider3).Select(static (t, _) => (t.Left.Left, t.Left.Right, t.Right));

        public static IncrementalValuesProvider<(TItem1, TItem2, TItem3, TItem4)> Combine<TItem1, TItem2, TItem3, TItem4>(this IncrementalValuesProvider<TItem1> provider1, IncrementalValueProvider<TItem2> provider2, IncrementalValueProvider<TItem3> provider3, IncrementalValueProvider<TItem4> provider4) => provider1.Combine(provider2).Combine(provider3).Combine(provider4).Select(static (t, _) => (t.Left.Left.Left, t.Left.Left.Right, t.Left.Right, t.Right));

        public static IncrementalValuesProvider<(TItem1, TItem2, TItem3, TItem4, TItem5)> Combine<TItem1, TItem2, TItem3, TItem4, TItem5>(this IncrementalValuesProvider<TItem1> provider1, IncrementalValueProvider<TItem2> provider2, IncrementalValueProvider<TItem3> provider3, IncrementalValueProvider<TItem4> provider4, IncrementalValueProvider<TItem5> provider5) => provider1.Combine(provider2).Combine(provider3).Combine(provider4).Combine(provider5).Select(static (t, _) => (t.Left.Left.Left.Left, t.Left.Left.Left.Right, t.Left.Left.Right, t.Left.Right, t.Right));

        public static IncrementalValuesProvider<(TItem1, TItem2, TItem3, TItem4, TItem5, TItem6)> Combine<TItem1, TItem2, TItem3, TItem4, TItem5, TItem6>(this IncrementalValuesProvider<TItem1> provider1, IncrementalValueProvider<TItem2> provider2, IncrementalValueProvider<TItem3> provider3, IncrementalValueProvider<TItem4> provider4, IncrementalValueProvider<TItem5> provider5, IncrementalValueProvider<TItem6> provider6) => provider1.Combine(provider2).Combine(provider3).Combine(provider4).Combine(provider5).Combine(provider6).Select(static (t, _) => (t.Left.Left.Left.Left.Left, t.Left.Left.Left.Left.Right, t.Left.Left.Left.Right, t.Left.Left.Right, t.Left.Right, t.Right));

        public static IncrementalValuesProvider<(TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7)> Combine<TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7>(this IncrementalValuesProvider<TItem1> provider1, IncrementalValueProvider<TItem2> provider2, IncrementalValueProvider<TItem3> provider3, IncrementalValueProvider<TItem4> provider4, IncrementalValueProvider<TItem5> provider5, IncrementalValueProvider<TItem6> provider6, IncrementalValueProvider<TItem7> provider7) => provider1.Combine(provider2).Combine(provider3).Combine(provider4).Combine(provider5).Combine(provider6).Combine(provider7).Select(static (t, _) => (t.Left.Left.Left.Left.Left.Left, t.Left.Left.Left.Left.Left.Right, t.Left.Left.Left.Left.Right, t.Left.Left.Left.Right, t.Left.Left.Right, t.Left.Right, t.Right));

        public static IncrementalValuesProvider<(TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8)> Combine<TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8>(this IncrementalValuesProvider<TItem1> provider1, IncrementalValueProvider<TItem2> provider2, IncrementalValueProvider<TItem3> provider3, IncrementalValueProvider<TItem4> provider4, IncrementalValueProvider<TItem5> provider5, IncrementalValueProvider<TItem6> provider6, IncrementalValueProvider<TItem7> provider7, IncrementalValueProvider<TItem8> provider8) => provider1.Combine(provider2).Combine(provider3).Combine(provider4).Combine(provider5).Combine(provider6).Combine(provider7).Combine(provider8).Select(static (t, _) => (t.Left.Left.Left.Left.Left.Left.Left, t.Left.Left.Left.Left.Left.Left.Right, t.Left.Left.Left.Left.Left.Right, t.Left.Left.Left.Left.Right, t.Left.Left.Left.Right, t.Left.Left.Right, t.Left.Right, t.Right));

        public static IncrementalValueProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(this IncrementalValueProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2) => new IncrementalValueProvider<(TLeft, TRight)>(new CombineNode<TLeft, TRight>(provider1.Node, provider2.Node), provider1.CatchAnalyzerExceptions);

        public static IncrementalValueProvider<(TItem1, TItem2, TItem3)> Combine<TItem1, TItem2, TItem3>(this IncrementalValueProvider<TItem1> provider1, IncrementalValueProvider<TItem2> provider2, IncrementalValueProvider<TItem3> provider3) => provider1.Combine(provider2).Combine(provider3).Select(static (t, _) => (t.Left.Left, t.Left.Right, t.Right));

        public static IncrementalValueProvider<(TItem1, TItem2, TItem3, TItem4)> Combine<TItem1, TItem2, TItem3, TItem4>(this IncrementalValueProvider<TItem1> provider1, IncrementalValueProvider<TItem2> provider2, IncrementalValueProvider<TItem3> provider3, IncrementalValueProvider<TItem4> provider4) => provider1.Combine(provider2).Combine(provider3).Combine(provider4).Select(static (t, _) => (t.Left.Left.Left, t.Left.Left.Right, t.Left.Right, t.Right));

        public static IncrementalValueProvider<(TItem1, TItem2, TItem3, TItem4, TItem5)> Combine<TItem1, TItem2, TItem3, TItem4, TItem5>(this IncrementalValueProvider<TItem1> provider1, IncrementalValueProvider<TItem2> provider2, IncrementalValueProvider<TItem3> provider3, IncrementalValueProvider<TItem4> provider4, IncrementalValueProvider<TItem5> provider5) => provider1.Combine(provider2).Combine(provider3).Combine(provider4).Combine(provider5).Select(static (t, _) => (t.Left.Left.Left.Left, t.Left.Left.Left.Right, t.Left.Left.Right, t.Left.Right, t.Right));

        public static IncrementalValueProvider<(TItem1, TItem2, TItem3, TItem4, TItem5, TItem6)> Combine<TItem1, TItem2, TItem3, TItem4, TItem5, TItem6>(this IncrementalValueProvider<TItem1> provider1, IncrementalValueProvider<TItem2> provider2, IncrementalValueProvider<TItem3> provider3, IncrementalValueProvider<TItem4> provider4, IncrementalValueProvider<TItem5> provider5, IncrementalValueProvider<TItem6> provider6) => provider1.Combine(provider2).Combine(provider3).Combine(provider4).Combine(provider5).Combine(provider6).Select(static (t, _) => (t.Left.Left.Left.Left.Left, t.Left.Left.Left.Left.Right, t.Left.Left.Left.Right, t.Left.Left.Right, t.Left.Right, t.Right));

        public static IncrementalValueProvider<(TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7)> Combine<TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7>(this IncrementalValueProvider<TItem1> provider1, IncrementalValueProvider<TItem2> provider2, IncrementalValueProvider<TItem3> provider3, IncrementalValueProvider<TItem4> provider4, IncrementalValueProvider<TItem5> provider5, IncrementalValueProvider<TItem6> provider6, IncrementalValueProvider<TItem7> provider7) => provider1.Combine(provider2).Combine(provider3).Combine(provider4).Combine(provider5).Combine(provider6).Combine(provider7).Select(static (t, _) => (t.Left.Left.Left.Left.Left.Left, t.Left.Left.Left.Left.Left.Right, t.Left.Left.Left.Left.Right, t.Left.Left.Left.Right, t.Left.Left.Right, t.Left.Right, t.Right));

        public static IncrementalValueProvider<(TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8)> Combine<TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8>(this IncrementalValueProvider<TItem1> provider1, IncrementalValueProvider<TItem2> provider2, IncrementalValueProvider<TItem3> provider3, IncrementalValueProvider<TItem4> provider4, IncrementalValueProvider<TItem5> provider5, IncrementalValueProvider<TItem6> provider6, IncrementalValueProvider<TItem7> provider7, IncrementalValueProvider<TItem8> provider8) => provider1.Combine(provider2).Combine(provider3).Combine(provider4).Combine(provider5).Combine(provider6).Combine(provider7).Combine(provider8).Select(static (t, _) => (t.Left.Left.Left.Left.Left.Left.Left, t.Left.Left.Left.Left.Left.Left.Right, t.Left.Left.Left.Left.Left.Right, t.Left.Left.Left.Left.Right, t.Left.Left.Left.Right, t.Left.Left.Right, t.Left.Right, t.Right));

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
