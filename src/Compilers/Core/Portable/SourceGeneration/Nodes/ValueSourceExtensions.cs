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
        /// <summary>
        /// Transforms an <see cref="IncrementalValueProvider{TSource}"/> into a new <see cref="IncrementalValueProvider{TResult}"/> by applying a transform function to the value.
        /// This is a 1-to-1 transformation where each input value produces exactly one output value.
        /// </summary>
        /// <typeparam name="TSource">The type of the input value</typeparam>
        /// <typeparam name="TResult">The type of the output value</typeparam>
        /// <param name="source">The input provider</param>
        /// <param name="selector">A function that transforms a <typeparamref name="TSource"/> into a <typeparamref name="TResult"/></param>
        /// <returns>A new <see cref="IncrementalValueProvider{TResult}"/> that provides the transformed value</returns>
        public static IncrementalValueProvider<TResult> Select<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector) => new IncrementalValueProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector, wrapUserFunc: source.CatchAnalyzerExceptions), source.CatchAnalyzerExceptions);

        /// <summary>
        /// Transforms an <see cref="IncrementalValuesProvider{TSource}"/> into a new <see cref="IncrementalValuesProvider{TResult}"/> by applying a transform function to each value.
        /// This is a 1-to-1 transformation where each input value produces exactly one output value.
        /// </summary>
        /// <typeparam name="TSource">The type of each input value</typeparam>
        /// <typeparam name="TResult">The type of each output value</typeparam>
        /// <param name="source">The input provider</param>
        /// <param name="selector">A function that transforms each <typeparamref name="TSource"/> into a <typeparamref name="TResult"/></param>
        /// <returns>A new <see cref="IncrementalValuesProvider{TResult}"/> that provides the transformed values</returns>
        public static IncrementalValuesProvider<TResult> Select<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector, wrapUserFunc: source.CatchAnalyzerExceptions), source.CatchAnalyzerExceptions);

        /// <summary>
        /// Transforms an <see cref="IncrementalValueProvider{TSource}"/> into a new <see cref="IncrementalValuesProvider{TResult}"/> by applying a transform function that returns zero or more results for the input value.
        /// This is a 1-to-many transformation where each input value can produce zero, one, or multiple output values.
        /// </summary>
        /// <typeparam name="TSource">The type of the input value</typeparam>
        /// <typeparam name="TResult">The type of each output value</typeparam>
        /// <param name="source">The input provider</param>
        /// <param name="selector">A function that transforms a <typeparamref name="TSource"/> into an <see cref="ImmutableArray{TResult}"/></param>
        /// <returns>A new <see cref="IncrementalValuesProvider{TResult}"/> that provides the transformed values</returns>
        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, ImmutableArray<TResult>> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector, wrapUserFunc: source.CatchAnalyzerExceptions), source.CatchAnalyzerExceptions);

        /// <summary>
        /// Transforms an <see cref="IncrementalValueProvider{TSource}"/> into a new <see cref="IncrementalValuesProvider{TResult}"/> by applying a transform function that returns zero or more results for the input value.
        /// This is a 1-to-many transformation where each input value can produce zero, one, or multiple output values.
        /// </summary>
        /// <typeparam name="TSource">The type of the input value</typeparam>
        /// <typeparam name="TResult">The type of each output value</typeparam>
        /// <param name="source">The input provider</param>
        /// <param name="selector">A function that transforms a <typeparamref name="TSource"/> into an <see cref="IEnumerable{TResult}"/></param>
        /// <returns>A new <see cref="IncrementalValuesProvider{TResult}"/> that provides the transformed values</returns>
        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, IEnumerable<TResult>> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector.WrapUserFunctionAsImmutableArray(source.CatchAnalyzerExceptions)), source.CatchAnalyzerExceptions);

        /// <summary>
        /// Transforms an <see cref="IncrementalValuesProvider{TSource}"/> into a new <see cref="IncrementalValuesProvider{TResult}"/> by applying a transform function that returns zero or more results for each input value.
        /// This is a many-to-many transformation where each input value can produce zero, one, or multiple output values.
        /// </summary>
        /// <typeparam name="TSource">The type of each input value</typeparam>
        /// <typeparam name="TResult">The type of each output value</typeparam>
        /// <param name="source">The input provider</param>
        /// <param name="selector">A function that transforms each <typeparamref name="TSource"/> into an <see cref="ImmutableArray{TResult}"/></param>
        /// <returns>A new <see cref="IncrementalValuesProvider{TResult}"/> that provides the transformed values</returns>
        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, ImmutableArray<TResult>> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector, wrapUserFunc: source.CatchAnalyzerExceptions), source.CatchAnalyzerExceptions);

        /// <summary>
        /// Transforms an <see cref="IncrementalValuesProvider{TSource}"/> into a new <see cref="IncrementalValuesProvider{TResult}"/> by applying a transform function that returns zero or more results for each input value.
        /// This is a many-to-many transformation where each input value can produce zero, one, or multiple output values.
        /// </summary>
        /// <typeparam name="TSource">The type of each input value</typeparam>
        /// <typeparam name="TResult">The type of each output value</typeparam>
        /// <param name="source">The input provider</param>
        /// <param name="selector">A function that transforms each <typeparamref name="TSource"/> into an <see cref="IEnumerable{TResult}"/></param>
        /// <returns>A new <see cref="IncrementalValuesProvider{TResult}"/> that provides the transformed values</returns>
        public static IncrementalValuesProvider<TResult> SelectMany<TSource, TResult>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, IEnumerable<TResult>> selector) => new IncrementalValuesProvider<TResult>(new TransformNode<TSource, TResult>(source.Node, selector.WrapUserFunctionAsImmutableArray(source.CatchAnalyzerExceptions)), source.CatchAnalyzerExceptions);

        /// <summary>
        /// Collects all values from an <see cref="IncrementalValuesProvider{TSource}"/> into a single <see cref="IncrementalValueProvider{T}"/> containing an <see cref="ImmutableArray{TSource}"/>.
        /// This is useful when you need to aggregate multiple values into a single collection to process them together.
        /// </summary>
        /// <typeparam name="TSource">The type of each value in the input provider</typeparam>
        /// <param name="source">The input provider with multiple values</param>
        /// <returns>A new <see cref="IncrementalValueProvider{T}"/> that provides an <see cref="ImmutableArray{TSource}"/> containing all values</returns>
        public static IncrementalValueProvider<ImmutableArray<TSource>> Collect<TSource>(this IncrementalValuesProvider<TSource> source) => new IncrementalValueProvider<ImmutableArray<TSource>>(new BatchNode<TSource>(source.Node), source.CatchAnalyzerExceptions);

        /// <summary>
        /// Combines an <see cref="IncrementalValuesProvider{TLeft}"/> with an <see cref="IncrementalValueProvider{TRight}"/> to create a new <see cref="IncrementalValuesProvider{T}"/> of tuples.
        /// Each value from the left provider is paired with the single value from the right provider.
        /// </summary>
        /// <typeparam name="TLeft">The type of each value in the left provider</typeparam>
        /// <typeparam name="TRight">The type of the value in the right provider</typeparam>
        /// <param name="provider1">The left provider with multiple values</param>
        /// <param name="provider2">The right provider with a single value</param>
        /// <returns>A new <see cref="IncrementalValuesProvider{T}"/> that provides tuples of (TLeft, TRight)</returns>
        public static IncrementalValuesProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(this IncrementalValuesProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2) => new IncrementalValuesProvider<(TLeft, TRight)>(new CombineNode<TLeft, TRight>(provider1.Node, provider2.Node), provider1.CatchAnalyzerExceptions);

        /// <summary>
        /// Combines two <see cref="IncrementalValueProvider{T}"/>s into a new <see cref="IncrementalValueProvider{T}"/> of a tuple.
        /// The single values from both providers are paired together.
        /// </summary>
        /// <typeparam name="TLeft">The type of the value in the left provider</typeparam>
        /// <typeparam name="TRight">The type of the value in the right provider</typeparam>
        /// <param name="provider1">The left provider with a single value</param>
        /// <param name="provider2">The right provider with a single value</param>
        /// <returns>A new <see cref="IncrementalValueProvider{T}"/> that provides a tuple of (TLeft, TRight)</returns>
        public static IncrementalValueProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(this IncrementalValueProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2) => new IncrementalValueProvider<(TLeft, TRight)>(new CombineNode<TLeft, TRight>(provider1.Node, provider2.Node), provider1.CatchAnalyzerExceptions);

        /// <summary>
        /// Filters values from an <see cref="IncrementalValuesProvider{TSource}"/> based on a predicate, producing a new <see cref="IncrementalValuesProvider{TSource}"/> containing only values that satisfy the predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of each value</typeparam>
        /// <param name="source">The input provider</param>
        /// <param name="predicate">A function that determines whether a value should be included in the output</param>
        /// <returns>A new <see cref="IncrementalValuesProvider{TSource}"/> that provides only values where the predicate returns <c>true</c></returns>
        public static IncrementalValuesProvider<TSource> Where<TSource>(this IncrementalValuesProvider<TSource> source, Func<TSource, bool> predicate) => source.SelectMany((item, _) => predicate(item) ? ImmutableArray.Create(item) : ImmutableArray<TSource>.Empty);

        internal static IncrementalValuesProvider<TSource> Where<TSource>(this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, bool> predicate) => source.SelectMany((item, c) => predicate(item, c) ? ImmutableArray.Create(item) : ImmutableArray<TSource>.Empty);

        /// <summary>
        /// Specifies a custom <see cref="IEqualityComparer{T}"/> to use when comparing values from this provider for caching purposes.
        /// By default, the generator infrastructure uses <see cref="EqualityComparer{T}.Default"/> to determine if values have changed.
        /// Use this method when you need custom equality logic, such as for complex objects or when you want to control when transformations are re-executed.
        /// </summary>
        /// <typeparam name="TSource">The type of the value</typeparam>
        /// <param name="source">The input provider</param>
        /// <param name="comparer">The custom equality comparer to use</param>
        /// <returns>A new <see cref="IncrementalValueProvider{TSource}"/> that uses the specified comparer</returns>
        public static IncrementalValueProvider<TSource> WithComparer<TSource>(this IncrementalValueProvider<TSource> source, IEqualityComparer<TSource> comparer) => new IncrementalValueProvider<TSource>(source.Node.WithComparer(comparer.WrapUserComparer(source.CatchAnalyzerExceptions)), source.CatchAnalyzerExceptions);

        /// <summary>
        /// Specifies a custom <see cref="IEqualityComparer{T}"/> to use when comparing values from this provider for caching purposes.
        /// By default, the generator infrastructure uses <see cref="EqualityComparer{T}.Default"/> to determine if values have changed.
        /// Use this method when you need custom equality logic, such as for complex objects or when you want to control when transformations are re-executed.
        /// </summary>
        /// <typeparam name="TSource">The type of each value</typeparam>
        /// <param name="source">The input provider</param>
        /// <param name="comparer">The custom equality comparer to use</param>
        /// <returns>A new <see cref="IncrementalValuesProvider{TSource}"/> that uses the specified comparer</returns>
        public static IncrementalValuesProvider<TSource> WithComparer<TSource>(this IncrementalValuesProvider<TSource> source, IEqualityComparer<TSource> comparer) => new IncrementalValuesProvider<TSource>(source.Node.WithComparer(comparer.WrapUserComparer(source.CatchAnalyzerExceptions)), source.CatchAnalyzerExceptions);

        /// <summary>
        /// Assigns a name to this provider step for tracking and debugging purposes.
        /// This name can be used in testing and diagnostic scenarios to understand the execution pipeline.
        /// </summary>
        /// <typeparam name="TSource">The type of the value</typeparam>
        /// <param name="source">The input provider</param>
        /// <param name="name">The tracking name to assign</param>
        /// <returns>A new <see cref="IncrementalValueProvider{TSource}"/> with the specified tracking name</returns>
        public static IncrementalValueProvider<TSource> WithTrackingName<TSource>(this IncrementalValueProvider<TSource> source, string name) => new IncrementalValueProvider<TSource>(source.Node.WithTrackingName(name), source.CatchAnalyzerExceptions);

        /// <summary>
        /// Assigns a name to this provider step for tracking and debugging purposes.
        /// This name can be used in testing and diagnostic scenarios to understand the execution pipeline.
        /// </summary>
        /// <typeparam name="TSource">The type of each value</typeparam>
        /// <param name="source">The input provider</param>
        /// <param name="name">The tracking name to assign</param>
        /// <returns>A new <see cref="IncrementalValuesProvider{TSource}"/> with the specified tracking name</returns>
        public static IncrementalValuesProvider<TSource> WithTrackingName<TSource>(this IncrementalValuesProvider<TSource> source, string name) => new IncrementalValuesProvider<TSource>(source.Node.WithTrackingName(name), source.CatchAnalyzerExceptions);
    }
}
