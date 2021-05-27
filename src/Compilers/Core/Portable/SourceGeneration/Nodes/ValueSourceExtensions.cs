// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    public static class IncrementalValueSourceExtensions
    {
        // 1 => 1 transform 
        public static IncrementalValueProvider<U> Transform<T, U>(this IncrementalValueProvider<T> source, Func<T, U> func) => new IncrementalValueProvider<U>(new TransformNode<T, U>(source.Node, func.WrapUserFunction()));

        // 1 => many (or none) transform
        public static IncrementalValueProvider<U> TransformMany<T, U>(this IncrementalValueProvider<T> source, Func<T, ImmutableArray<U>> func) => new IncrementalValueProvider<U>(new TransformNode<T, U>(source.Node, func.WrapUserFunction()));

        // 1 => many (or none) transform with enumerable
        public static IncrementalValueProvider<U> TransformMany<T, U>(this IncrementalValueProvider<T> source, Func<T, IEnumerable<U>> func) => new IncrementalValueProvider<U>(new TransformNode<T, U>(source.Node, t => func.WrapUserFunction()(t).AsImmutable()));

        // collection => collection
        public static IncrementalValueProvider<U> BatchTransform<T, U>(this IncrementalValueProvider<T> source, Func<ImmutableArray<T>, U> func) => new IncrementalValueProvider<U>(new BatchTransformNode<T, U>(source.Node, func.WrapUserFunction()));

        // single
        public static IncrementalValueProvider<U> BatchTransformMany<T, U>(this IncrementalValueProvider<T> source, Func<ImmutableArray<T>, ImmutableArray<U>> func) => new IncrementalValueProvider<U>(new BatchTransformNode<T, U>(source.Node, func.WrapUserFunction()));

        // single (enumerable)
        public static IncrementalValueProvider<U> BatchTransformMany<T, U>(this IncrementalValueProvider<T> source, Func<ImmutableArray<T>, IEnumerable<U>> func) => new IncrementalValueProvider<U>(new BatchTransformNode<T, U>(source.Node, t => func.WrapUserFunction()(t).ToImmutableArray()));


        // join many => many ((source1[0], source2), (source1[0], source2) ...)
        public static IncrementalValueProvider<(T, ImmutableArray<U>)> Join<T, U>(this IncrementalValueProvider<T> source1, IncrementalValueProvider<U> source2) => new IncrementalValueProvider<(T, ImmutableArray<U>)>(new JoinNode<T, U>(source1.Node, source2.Node));

        // helper for filtering
        public static IncrementalValueProvider<T> Filter<T>(this IncrementalValueProvider<T> source, Func<T, bool> filter)
        {
            return source.TransformMany((item) => filter(item) ? ImmutableArray.Create(item) : ImmutableArray<T>.Empty);
        }

        // 1 => 1 production
        public static void GenerateSource<T>(this IncrementalValueProvider<T> source, Action<SourceProductionContext, T> action) => source.Node.RegisterOutput(new SourceOutputNode<T>(source.Node, action.WrapUserAction()));

        // custom comparer for given node
        public static IncrementalValueProvider<T> WithComparer<T>(this IncrementalValueProvider<T> source, IEqualityComparer<T> comparer) => new IncrementalValueProvider<T>(source.Node.WithComparer(comparer));
    }
}
